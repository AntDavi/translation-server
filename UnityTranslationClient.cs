using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket; // Biblioteca: https://github.com/endel/NativeWebSocket

/// <summary>
/// Cliente WebSocket para integração com o servidor de tradução em tempo real.
/// 
/// INSTALAÇÃO:
/// 1. Adicione o NativeWebSocket via Package Manager:
///    https://github.com/endel/NativeWebSocket.git#upm
/// 2. Ou instale via Unity Package Manager usando Git URL
/// 
/// USO:
/// 1. Adicione este script a um GameObject na cena
/// 2. Configure os parâmetros no Inspector
/// 3. Chame Connect() para iniciar a conexão
/// 4. Use SendUtterance() para enviar mensagens
/// </summary>
public class UnityTranslationClient : MonoBehaviour
{
    [Header("Configurações do Servidor")]
    [Tooltip("URL do servidor WebSocket (ex: ws://localhost:8080)")]
    public string serverUrl = "ws://localhost:8080";

    [Header("Configurações do Cliente")]
    [Tooltip("ID único deste cliente")]
    public string clientId = "unity-player-1";

    [Tooltip("ID da sala para entrar")]
    public string roomId = "room-1";

    [Tooltip("Idioma deste cliente (pt-BR, en-US, es-ES, etc)")]
    public string language = "pt-BR";

    [Header("Configurações de Reconexão")]
    [Tooltip("Tentar reconectar automaticamente")]
    public bool autoReconnect = true;

    [Tooltip("Intervalo entre tentativas de reconexão (segundos)")]
    public float reconnectInterval = 5f;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    // WebSocket
    private WebSocket websocket;
    private bool isConnected = false;
    private bool isJoined = false;
    private bool shouldReconnect = false;

    // Eventos
    public event Action OnConnected;
    public event Action<string> OnJoinedRoom;
    public event Action<TranscriptionMessage> OnTranscriptionReceived;
    public event Action<string> OnError;
    public event Action OnDisconnected;

    #region Unity Lifecycle

    void Start()
    {
        // Gerar ID único se não foi definido
        if (string.IsNullOrEmpty(clientId))
        {
            clientId = $"unity-{Guid.NewGuid().ToString().Substring(0, 8)}";
        }

        LogDebug($"Cliente inicializado: {clientId}");
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        // Processar mensagens da fila (necessário em plataformas não-WebGL)
        if (websocket != null)
        {
            websocket.DispatchMessageQueue();
        }
#endif
    }

    void OnApplicationQuit()
    {
        shouldReconnect = false;
        DisconnectAsync();
    }

    void OnDestroy()
    {
        shouldReconnect = false;
        DisconnectAsync();
    }

    #endregion

    #region Conexão

    /// <summary>
    /// Conecta ao servidor WebSocket e entra na sala especificada
    /// </summary>
    public async void Connect()
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            LogDebug("Já está conectado!");
            return;
        }

        shouldReconnect = autoReconnect;

        try
        {
            LogDebug($"Conectando ao servidor: {serverUrl}");

            websocket = new WebSocket(serverUrl);

            // Event Handlers
            websocket.OnOpen += () =>
            {
                isConnected = true;
                LogDebug("WebSocket conectado!");
                OnConnected?.Invoke();

                // Automaticamente entra na sala após conectar
                SendJoinMessage();
            };

            websocket.OnMessage += (bytes) =>
            {
                string message = System.Text.Encoding.UTF8.GetString(bytes);
                HandleMessage(message);
            };

            websocket.OnError += (error) =>
            {
                LogError($"WebSocket Error: {error}");
                OnError?.Invoke(error);
            };

            websocket.OnClose += (code) =>
            {
                isConnected = false;
                isJoined = false;
                LogDebug($"WebSocket desconectado. Código: {code}");
                OnDisconnected?.Invoke();

                // Tentar reconectar se habilitado
                if (shouldReconnect)
                {
                    StartCoroutine(ReconnectCoroutine());
                }
            };

            await websocket.Connect();
        }
        catch (Exception ex)
        {
            LogError($"Erro ao conectar: {ex.Message}");
            OnError?.Invoke(ex.Message);

            if (shouldReconnect)
            {
                StartCoroutine(ReconnectCoroutine());
            }
        }
    }

    /// <summary>
    /// Desconecta do servidor WebSocket
    /// </summary>
    public async void DisconnectAsync()
    {
        shouldReconnect = false;

        if (websocket != null)
        {
            await websocket.Close();
            websocket = null;
            isConnected = false;
            isJoined = false;
            LogDebug("Desconectado do servidor");
        }
    }

    /// <summary>
    /// Coroutine para reconexão automática
    /// </summary>
    private IEnumerator ReconnectCoroutine()
    {
        LogDebug($"Tentando reconectar em {reconnectInterval} segundos...");
        yield return new WaitForSeconds(reconnectInterval);

        if (shouldReconnect && !isConnected)
        {
            Connect();
        }
    }

    #endregion

    #region Envio de Mensagens

    /// <summary>
    /// Envia mensagem JOIN para entrar na sala
    /// </summary>
    private void SendJoinMessage()
    {
        var joinMsg = new JoinMessage
        {
            type = "join",
            clientId = this.clientId,
            roomId = this.roomId,
            language = this.language
        };

        string json = JsonUtility.ToJson(joinMsg);
        SendMessage(json);
        LogDebug($"JOIN enviado: {json}");
    }

    /// <summary>
    /// Envia uma mensagem de fala (utterance) para ser traduzida
    /// </summary>
    /// <param name="text">Texto a ser traduzido</param>
    /// <param name="utteranceId">ID único da mensagem (opcional)</param>
    public void SendUtterance(string text, string utteranceId = null)
    {
        if (!isConnected || !isJoined)
        {
            LogError("Não está conectado ou não entrou na sala!");
            return;
        }

        if (string.IsNullOrEmpty(text))
        {
            LogError("Texto vazio!");
            return;
        }

        // Gerar ID único se não fornecido
        if (string.IsNullOrEmpty(utteranceId))
        {
            utteranceId = $"utt-{Guid.NewGuid().ToString().Substring(0, 8)}";
        }

        var utteranceMsg = new UtteranceMessage
        {
            type = "utterance",
            utteranceId = utteranceId,
            speakerId = this.clientId,
            roomId = this.roomId,
            language = this.language,
            text = text
        };

        string json = JsonUtility.ToJson(utteranceMsg);
        SendMessage(json);
        LogDebug($"UTTERANCE enviado: {text}");
    }

    /// <summary>
    /// Envia mensagem crua para o servidor
    /// </summary>
    private async void SendMessage(string message)
    {
        if (websocket == null || websocket.State != WebSocketState.Open)
        {
            LogError("WebSocket não está aberto!");
            return;
        }

        await websocket.SendText(message);
    }

    #endregion

    #region Recebimento de Mensagens

    /// <summary>
    /// Processa mensagens recebidas do servidor
    /// </summary>
    private void HandleMessage(string message)
    {
        LogDebug($"Mensagem recebida: {message}");

        try
        {
            // Parse básico para identificar o tipo
            var baseMsg = JsonUtility.FromJson<BaseMessage>(message);

            switch (baseMsg.type)
            {
                case "joined":
                    HandleJoinedMessage(message);
                    break;

                case "transcription":
                    HandleTranscriptionMessage(message);
                    break;

                case "error":
                    HandleErrorMessage(message);
                    break;

                default:
                    LogDebug($"Tipo de mensagem desconhecido: {baseMsg.type}");
                    break;
            }
        }
        catch (Exception ex)
        {
            LogError($"Erro ao processar mensagem: {ex.Message}");
        }
    }

    /// <summary>
    /// Processa confirmação de entrada na sala
    /// </summary>
    private void HandleJoinedMessage(string message)
    {
        var joinedMsg = JsonUtility.FromJson<JoinedMessage>(message);
        isJoined = true;
        LogDebug($"Entrou na sala: {joinedMsg.roomId}");
        OnJoinedRoom?.Invoke(joinedMsg.roomId);
    }

    /// <summary>
    /// Processa mensagem de transcrição/tradução
    /// </summary>
    private void HandleTranscriptionMessage(string message)
    {
        var transcription = JsonUtility.FromJson<TranscriptionMessage>(message);
        LogDebug($"Tradução recebida de {transcription.speakerId}: {transcription.text}");
        OnTranscriptionReceived?.Invoke(transcription);
    }

    /// <summary>
    /// Processa mensagem de erro
    /// </summary>
    private void HandleErrorMessage(string message)
    {
        var errorMsg = JsonUtility.FromJson<ErrorMessage>(message);
        LogError($"Erro do servidor: {errorMsg.message}");
        OnError?.Invoke(errorMsg.message);
    }

    #endregion

    #region Utilidades

    /// <summary>
    /// Retorna se está conectado ao servidor
    /// </summary>
    public bool IsConnected()
    {
        return isConnected && websocket != null && websocket.State == WebSocketState.Open;
    }

    /// <summary>
    /// Retorna se entrou em uma sala
    /// </summary>
    public bool IsInRoom()
    {
        return isJoined;
    }

    /// <summary>
    /// Altera a sala (desconecta e reconecta)
    /// </summary>
    public void ChangeRoom(string newRoomId)
    {
        this.roomId = newRoomId;
        DisconnectAsync();
        Connect();
    }

    /// <summary>
    /// Altera o idioma (desconecta e reconecta)
    /// </summary>
    public void ChangeLanguage(string newLanguage)
    {
        this.language = newLanguage;
        DisconnectAsync();
        Connect();
    }

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[TranslationClient] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[TranslationClient] {message}");
    }

    #endregion
}

#region Message Classes

/// <summary>
/// Classe base para todas as mensagens
/// </summary>
[Serializable]
public class BaseMessage
{
    public string type;
}

/// <summary>
/// Mensagem de entrada na sala
/// </summary>
[Serializable]
public class JoinMessage
{
    public string type;
    public string clientId;
    public string roomId;
    public string language;
}

/// <summary>
/// Confirmação de entrada na sala
/// </summary>
[Serializable]
public class JoinedMessage
{
    public string type;
    public string clientId;
    public string roomId;
}

/// <summary>
/// Mensagem de fala/utterance
/// </summary>
[Serializable]
public class UtteranceMessage
{
    public string type;
    public string utteranceId;
    public string speakerId;
    public string roomId;
    public string language;
    public string text;
}

/// <summary>
/// Mensagem de transcrição/tradução recebida
/// </summary>
[Serializable]
public class TranscriptionMessage
{
    public string type;
    public string utteranceId;
    public string speakerId;
    public string roomId;
    public string originalLanguage;
    public string targetLanguage;
    public string text;
}

/// <summary>
/// Mensagem de erro
/// </summary>
[Serializable]
public class ErrorMessage
{
    public string type;
    public string message;
}

#endregion

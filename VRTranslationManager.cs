using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Gerenciador de tradução para VR Multiplayer Sample
/// Conecta ao servidor de tradução quando o jogador entra na rede
/// Compatível com Unity 2022.3.48f1 e Netcode for GameObjects
/// </summary>
public class VRTranslationManager : NetworkBehaviour
{
    [Header("Configurações")]
    public UnityTranslationClient translationClient;

    [Header("Idioma do Jogador")]
    [Tooltip("Idioma preferido deste jogador (pt-BR, en-US, es-ES, etc)")]
    public string playerLanguage = "pt-BR";

    [Header("Sincronização de Sala")]
    [Tooltip("Usar ID da sessão de rede como roomId?")]
    public bool useSyncedRoomId = true;

    [Tooltip("Room ID manual (usado se useSyncedRoomId = false)")]
    public string manualRoomId = "vr-multiplayer-room";

    [Header("Debug")]
    public bool showDebugLogs = true;

    private string assignedRoomId;
    private bool isInitialized = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Apenas o dono do objeto executa
        if (!IsOwner)
        {
            LogDebug("Não é o dono, ignorando inicialização");
            return;
        }

        LogDebug("VRTranslationManager: Network spawned para owner");

        // Aguardar 1 frame para garantir que NetworkManager está pronto
        Invoke(nameof(InitializeTranslation), 0.1f);
    }

    private void InitializeTranslation()
    {
        if (isInitialized) return;

        // Configurar cliente de tradução
        ConfigureTranslationClient();

        // Conectar ao servidor de tradução
        ConnectToTranslationServer();

        isInitialized = true;
    }

    private void ConfigureTranslationClient()
    {
        if (translationClient == null)
        {
            LogError("TranslationClient não está atribuído!");
            return;
        }

        // Usar o NetworkObject ID como base para clientId único
        ulong networkId = NetworkManager.Singleton.LocalClientId;
        translationClient.clientId = $"vr-player-{networkId}";

        // Determinar roomId
        if (useSyncedRoomId)
        {
            // Criar roomId baseado no ID da sessão ou host
            assignedRoomId = $"vr-session-{NetworkManager.Singleton.LocalClientId}";
        }
        else
        {
            assignedRoomId = manualRoomId;
        }

        translationClient.roomId = assignedRoomId;

        // Definir idioma
        translationClient.language = playerLanguage;

        LogDebug($"Cliente configurado: {translationClient.clientId}, Sala: {assignedRoomId}, Idioma: {playerLanguage}");
    }

    private void ConnectToTranslationServer()
    {
        if (translationClient == null)
        {
            LogError("TranslationClient é null!");
            return;
        }

        if (translationClient.IsConnected())
        {
            LogDebug("Já está conectado ao servidor de tradução");
            return;
        }

        LogDebug("Conectando ao servidor de tradução...");
        translationClient.Connect();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsOwner && translationClient != null)
        {
            LogDebug("Desconectando do servidor de tradução...");
            translationClient.DisconnectAsync();
            isInitialized = false;
        }
    }

    /// <summary>
    /// Envia mensagem para tradução (chamado por UI ou voice input)
    /// </summary>
    public void SendTranslationMessage(string message)
    {
        if (!IsOwner)
        {
            LogError("Apenas o dono pode enviar mensagens!");
            return;
        }

        if (string.IsNullOrEmpty(message))
        {
            LogError("Mensagem vazia!");
            return;
        }

        if (translationClient != null && translationClient.IsConnected())
        {
            translationClient.SendUtterance(message);
            LogDebug($"Mensagem enviada para tradução: {message}");
        }
        else
        {
            LogError("Cliente de tradução não está conectado!");

            // Tentar reconectar
            if (!isInitialized)
            {
                InitializeTranslation();
            }
        }
    }

    /// <summary>
    /// Altera o idioma do jogador
    /// </summary>
    public void ChangePlayerLanguage(string newLanguage)
    {
        if (!IsOwner) return;

        playerLanguage = newLanguage;

        if (translationClient != null)
        {
            translationClient.ChangeLanguage(newLanguage);
            LogDebug($"Idioma alterado para: {newLanguage}");
        }
    }

    /// <summary>
    /// Verifica se está conectado ao servidor de tradução
    /// </summary>
    public bool IsConnectedToTranslation()
    {
        return translationClient != null && translationClient.IsConnected();
    }

    /// <summary>
    /// Obtém o idioma atual
    /// </summary>
    public string GetCurrentLanguage()
    {
        return playerLanguage;
    }

    /// <summary>
    /// Obtém o ID da sala atual
    /// </summary>
    public string GetCurrentRoomId()
    {
        return assignedRoomId;
    }

    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[VRTranslationManager] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[VRTranslationManager] {message}");
    }
}

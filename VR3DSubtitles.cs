using UnityEngine;
using TMPro;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Exibe legendas 3D no espaço VR próximas aos jogadores
/// Compatível com Unity 2022.3.48f1 e VR Multiplayer Sample
/// </summary>
public class VR3DSubtitles : NetworkBehaviour
{
    [Header("Referências")]
    public UnityTranslationClient translationClient;
    public Transform vrCamera; // XR Origin Camera

    [Header("Configurações de Legenda")]
    public GameObject subtitlePrefab; // Prefab com Canvas + TextMeshPro
    public float subtitleDuration = 5f;
    public float subtitleDistance = 2f;
    public float subtitleHeight = 0.5f;
    public float subtitleFontSize = 24f;

    [Header("Cores")]
    public Color ownMessageColor = Color.white;
    public Color otherMessageColor = Color.yellow;
    public Color systemMessageColor = Color.cyan;

    [Header("Opções")]
    [Tooltip("Exibir tag de idioma nas mensagens traduzidas")]
    public bool showLanguageTag = true;

    [Tooltip("Suavizar movimento da legenda")]
    public bool smoothFollow = true;
    public float followSpeed = 5f;

    private GameObject currentSubtitle;
    private TMP_Text currentSubtitleText;
    private Canvas subtitleCanvas;
    private Coroutine hideCoroutine;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    void Start()
    {
        // Apenas o owner gerencia suas próprias legendas
        if (!IsOwner) return;

        if (translationClient != null)
        {
            // Registrar callbacks para traduções recebidas
            translationClient.OnTranscriptionReceived += OnTranscriptionReceived;
            translationClient.OnJoinedRoom += OnJoinedRoom;
            translationClient.OnError += OnTranslationError;
        }
        else
        {
            Debug.LogError("[VR3DSubtitles] TranslationClient não atribuído!");
        }

        // Encontrar câmera VR se não atribuída
        if (vrCamera == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                vrCamera = mainCam.transform;
            }
            else
            {
                Debug.LogWarning("[VR3DSubtitles] VR Camera não encontrada!");
            }
        }

        // Criar objeto de legenda
        CreateSubtitleObject();
    }

    void OnDestroy()
    {
        if (translationClient != null)
        {
            translationClient.OnTranscriptionReceived -= OnTranscriptionReceived;
            translationClient.OnJoinedRoom -= OnJoinedRoom;
            translationClient.OnError -= OnTranslationError;
        }

        if (currentSubtitle != null)
        {
            Destroy(currentSubtitle);
        }
    }

    private void OnJoinedRoom(string roomId)
    {
        ShowSubtitle($"Conectado à sala: {roomId}", systemMessageColor, 3f);
    }

    private void OnTranslationError(string error)
    {
        ShowSubtitle($"Erro: {error}", Color.red, 3f);
    }

    private void OnTranscriptionReceived(TranscriptionMessage msg)
    {
        // Determinar cor baseado se é mensagem própria
        Color messageColor = (msg.speakerId == translationClient.clientId)
            ? ownMessageColor
            : otherMessageColor;

        // Formatar mensagem
        string displayText = FormatMessage(msg);

        // Exibir legenda
        ShowSubtitle(displayText, messageColor, subtitleDuration);
    }

    private string FormatMessage(TranscriptionMessage msg)
    {
        string senderName = msg.speakerId;

        // Adicionar tag de idioma se foi traduzido e opção ativa
        if (showLanguageTag && msg.originalLanguage != msg.targetLanguage)
        {
            senderName += $" [{msg.targetLanguage}]";
        }

        return $"{senderName}: {msg.text}";
    }

    private void ShowSubtitle(string text, Color color, float duration)
    {
        if (currentSubtitle == null)
        {
            CreateSubtitleObject();
        }

        if (currentSubtitleText != null)
        {
            currentSubtitleText.text = text;
            currentSubtitleText.color = color;
        }

        // Posicionar legenda na frente da câmera VR
        if (vrCamera != null)
        {
            UpdateTargetPosition();
        }

        // Mostrar legenda
        currentSubtitle.SetActive(true);

        // Cancelar hide anterior e agendar novo
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }
        hideCoroutine = StartCoroutine(HideSubtitleAfterDelay(duration));
    }

    private void CreateSubtitleObject()
    {
        if (subtitlePrefab != null)
        {
            // Usar prefab fornecido
            currentSubtitle = Instantiate(subtitlePrefab);
            currentSubtitle.name = "VR_Subtitle_Instance";
        }
        else
        {
            // Criar legenda proceduralmente
            currentSubtitle = new GameObject("VR_Subtitle");

            // Adicionar Canvas
            subtitleCanvas = currentSubtitle.AddComponent<Canvas>();
            subtitleCanvas.renderMode = RenderMode.WorldSpace;

            // Configurar RectTransform do Canvas
            RectTransform canvasRect = currentSubtitle.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(4, 1); // 4m x 1m no mundo

            // Criar objeto de texto
            GameObject textObj = new GameObject("SubtitleText");
            textObj.transform.SetParent(currentSubtitle.transform, false);

            // Adicionar TextMeshPro
            currentSubtitleText = textObj.AddComponent<TextMeshProUGUI>();
            currentSubtitleText.fontSize = subtitleFontSize;
            currentSubtitleText.alignment = TextAlignmentOptions.Center;
            currentSubtitleText.enableWordWrapping = true;
            currentSubtitleText.overflowMode = TextOverflowModes.Overflow;

            // Configurar RectTransform do texto
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            // Adicionar sombra para melhor legibilidade
            var shadow = textObj.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(2, -2);
        }

        // Buscar componente de texto se não foi encontrado
        if (currentSubtitleText == null)
        {
            currentSubtitleText = currentSubtitle.GetComponentInChildren<TMP_Text>();
        }

        if (subtitleCanvas == null)
        {
            subtitleCanvas = currentSubtitle.GetComponent<Canvas>();
        }

        // Inicialmente invisível
        currentSubtitle.SetActive(false);

        Debug.Log("[VR3DSubtitles] Objeto de legenda criado");
    }

    private void UpdateTargetPosition()
    {
        if (vrCamera == null) return;

        // Calcular posição na frente da câmera
        Vector3 forward = vrCamera.forward;
        forward.y = 0; // Manter no plano horizontal
        forward.Normalize();

        targetPosition = vrCamera.position + forward * subtitleDistance;
        targetPosition.y = vrCamera.position.y - subtitleHeight;

        // Calcular rotação para olhar para a câmera
        Vector3 lookDir = vrCamera.position - targetPosition;
        lookDir.y = 0;

        if (lookDir != Vector3.zero)
        {
            targetRotation = Quaternion.LookRotation(lookDir);
        }
    }

    private void PositionSubtitle()
    {
        if (currentSubtitle == null) return;

        if (smoothFollow)
        {
            // Movimento suave
            currentSubtitle.transform.position = Vector3.Lerp(
                currentSubtitle.transform.position,
                targetPosition,
                Time.deltaTime * followSpeed
            );

            currentSubtitle.transform.rotation = Quaternion.Slerp(
                currentSubtitle.transform.rotation,
                targetRotation,
                Time.deltaTime * followSpeed
            );
        }
        else
        {
            // Movimento imediato
            currentSubtitle.transform.position = targetPosition;
            currentSubtitle.transform.rotation = targetRotation;
        }
    }

    private IEnumerator HideSubtitleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (currentSubtitle != null)
        {
            currentSubtitle.SetActive(false);
        }
    }

    void Update()
    {
        // Apenas o owner atualiza
        if (!IsOwner) return;

        // Atualizar posição da legenda para seguir a câmera
        if (currentSubtitle != null && currentSubtitle.activeSelf && vrCamera != null)
        {
            UpdateTargetPosition();
            PositionSubtitle();
        }
    }

    /// <summary>
    /// Mostra uma mensagem customizada (útil para debug)
    /// </summary>
    public void ShowCustomMessage(string message, float duration = 3f)
    {
        ShowSubtitle(message, Color.white, duration);
    }

    /// <summary>
    /// Esconde a legenda imediatamente
    /// </summary>
    public void HideSubtitleNow()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        if (currentSubtitle != null)
        {
            currentSubtitle.SetActive(false);
        }
    }
}

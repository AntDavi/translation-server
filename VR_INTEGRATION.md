# Integração Unity VR

Guia de integração para Unity 2022.3.48f1 com VR Multiplayer Sample.

---

## Setup Rápido

### 1. Instalar NativeWebSocket

```
Unity > Package Manager > + > Add package from git URL
https://github.com/endel/NativeWebSocket.git#upm
```

### 2. Adicionar Scripts

Copie para `Assets/Scripts/Translation/`:
- `UnityTranslationClient.cs`
- `VRTranslationManager.cs`
- `VR3DSubtitles.cs`

### 3. Configurar Player Prefab

```
PlayerVR_Prefab
└── TranslationManager (Empty GameObject)
    ├── UnityTranslationClient (component)
    ├── VRTranslationManager (component)
    └── VR3DSubtitles (component)
```

**Inspector - UnityTranslationClient:**
- Server URL: `ws://SEU_IP:8080`
- Client ID: (deixe vazio - gerado automaticamente)
- Room ID: `vr-multiplayer-room`
- Language: `pt-BR`

**Inspector - VRTranslationManager:**
- Translation Client: arraste `UnityTranslationClient`
- Player Language: `pt-BR`

**Inspector - VR3DSubtitles:**
- Translation Client: arraste `UnityTranslationClient`
- VR Camera: arraste `Main Camera` do XR Origin

---

## Scripts

### UnityTranslationClient.cs
Cliente WebSocket base. Gerencia conexão, envio e recebimento de mensagens.

**Eventos principais:**
- `OnConnected` - Conectou ao servidor
- `OnJoinedRoom` - Entrou na sala
- `OnTranscriptionReceived` - Recebeu tradução
- `OnError` - Erro ocorreu
- `OnDisconnected` - Desconectou

**Métodos principais:**
- `Connect()` - Conecta ao servidor
- `SendUtterance(string text)` - Envia mensagem
- `IsConnected()` - Verifica status

### VRTranslationManager.cs
Integração com Netcode for GameObjects. Conecta automaticamente quando jogador spawna na rede.

```csharp
public override void OnNetworkSpawn()
{
    if (!IsOwner) return;
    
    // Configura clientId baseado em NetworkManager
    translationClient.clientId = $"vr-player-{OwnerClientId}";
    translationClient.Connect();
}
```

**Para enviar mensagem:**
```csharp
GetComponent<VRTranslationManager>().SendTranslationMessage("Olá!");
```

### VR3DSubtitles.cs
Exibe legendas 3D no espaço VR, sempre na frente da câmera.

**Características:**
- Posicionamento automático na frente do headset
- Cores diferentes para mensagens próprias/outros
- Fade out automático após 5 segundos
- Smooth follow da câmera VR

---

## Uso Básico

### Enviar Mensagem

```csharp
// Via VRTranslationManager
VRTranslationManager manager = GetComponent<VRTranslationManager>();
manager.SendTranslationMessage("Olá pessoal!");

// Ou diretamente via cliente
UnityTranslationClient client = GetComponent<UnityTranslationClient>();
client.SendUtterance("Olá pessoal!");
```

### Receber Traduções

```csharp
void Start()
{
    translationClient.OnTranscriptionReceived += (msg) => {
        Debug.Log($"{msg.speakerId}: {msg.text}");
        // Legendas 3D são exibidas automaticamente por VR3DSubtitles
    };
}
```

### Mudar Idioma

```csharp
manager.ChangePlayerLanguage("en-US");
```

---

## Sincronização com Netcode

O sistema funciona paralelamente ao Netcode:
- **Netcode**: Sincroniza posição, ações, estado do jogo
- **WebSocket**: Envia/recebe mensagens de chat/voz traduzidas

Cada jogador conecta independentemente ao servidor de tradução, usando o mesmo `roomId`.

---

## Rede Local (Quest)

Para testar no Meta Quest via WiFi:

1. Descubra seu IP local:
   ```bash
   # Windows
   ipconfig
   
   # Mac/Linux
   ifconfig
   ```

2. No Unity, configure:
   ```
   Server URL: ws://192.168.1.X:8080
   ```

3. Certifique-se que Quest e PC estão na mesma rede WiFi

---

## Troubleshooting

**"Connection refused"**
- Servidor Node.js não está rodando
- Firewall bloqueando porta 8080
- IP/URL incorreto

**Legendas não aparecem**
- Verificar se VR Camera está atribuída no VR3DSubtitles
- Conferir distância (subtitleDistance = 2m)

**Mensagens não traduzem**
- Verificar credenciais Azure no .env do servidor
- Checar logs do servidor Node.js: `console.error`

**Quest não conecta**
- Usar IP da rede local, não `localhost`
- Verificar que está na mesma WiFi
- Testar conexão: `ws://192.168.1.X:8080`

---

## Integração com Voice Chat

Para usar com reconhecimento de voz:

1. Implementar Speech-to-Text (Azure Speech SDK)
2. Quando voz é reconhecida, enviar texto via `SendTranslationMessage()`
3. Traduções aparecem automaticamente nas legendas 3D

Exemplo básico:
```csharp
// Quando microfone detecta voz e converte para texto
void OnVoiceRecognized(string text)
{
    vrTranslationManager.SendTranslationMessage(text);
}
```

---

## Performance

**Otimizações:**
- Limitar mensagens: max 500 caracteres
- Rate limiting: 1 mensagem/segundo
- Desabilitar logs debug em produção:
  ```csharp
  translationClient.enableDebugLogs = false;
  ```

**Latência típica:**
- WebSocket: 10-50ms
- Azure Translator: 100-300ms
- **Total**: ~200-400ms por tradução

---

## Build para Quest (Android)

1. Build Settings > Android
2. Player Settings:
   - Internet Access: Required
   - Write Permission: External (SDCard)
3. XR Plug-in Management: Oculus habilitado
4. Build and Run

**Permissions (AndroidManifest.xml):**
```xml
<uses-permission android:name="android.permission.INTERNET" />
```

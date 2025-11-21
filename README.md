# Translation Server

Sistema de tradução em tempo real via WebSocket + Azure Translator para Unity VR Multiplayer.

---

## 🚀 Executar Servidor

```bash
# Instalar dependências
npm install

# Configurar credenciais Azure
cp .env.example .env
# Editar .env com suas chaves

# Iniciar servidor
node src/index.js
```

Servidor estará rodando em `ws://localhost:8080`

---

## ⚙️ Configuração (.env)

```env
PORT=8080
AZURE_TRANSLATOR_ENDPOINT=https://api.cognitive.microsofttranslator.com
AZURE_TRANSLATOR_KEY=sua_chave_aqui
AZURE_TRANSLATOR_REGION=sua_regiao
```

Obtenha credenciais em: https://portal.azure.com → Translator

---

## 📡 Protocolo WebSocket

### Entrar na Sala
```json
{
  "type": "join",
  "clientId": "player-1",
  "roomId": "room-abc",
  "language": "pt-BR"
}
```

### Enviar Mensagem
```json
{
  "type": "utterance",
  "utteranceId": "msg-001",
  "speakerId": "player-1",
  "roomId": "room-abc",
  "language": "pt-BR",
  "text": "Olá!"
}
```

### Receber Tradução
```json
{
  "type": "transcription",
  "utteranceId": "msg-001",
  "speakerId": "player-1",
  "roomId": "room-abc",
  "originalLanguage": "pt-BR",
  "targetLanguage": "en-US",
  "text": "Hello!"
}
```

---

## 📚 Documentação

- **[DOCUMENTACAO.md](./DOCUMENTACAO.md)** - Arquitetura e funcionamento
- **[VR_INTEGRATION.md](./VR_INTEGRATION.md)** - Integração Unity VR

---

## 🧪 Teste Rápido

```bash
# Terminal 1: Servidor
node src/index.js

# Terminal 2: Cliente de teste
node test-client.js
```

---

## 🛠️ Stack

- Node.js + WebSocket (ws)
- Azure Translator API
- Unity 2022.3.48f1 + Netcode + XR Toolkit

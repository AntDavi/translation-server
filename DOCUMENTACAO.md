# Documentação Técnica

Sistema de tradução em tempo real para VR Multiplayer usando WebSocket + Azure Translator.

---

## Arquitetura

```
Unity Client (VR)          Node.js Server          Azure Translator
     │                          │                         │
     │─── WebSocket ────────────│                         │
     │    {type: "join"}        │                         │
     │◄─── {type: "joined"} ────│                         │
     │                          │                         │
     │─── {type: "utterance"} ──│                         │
     │    {text: "Olá"}         │                         │
     │                          │──── Translate(pt→en) ───│
     │                          │◄─── "Hello" ────────────│
     │◄─── {type: "transcription"} ─│                     │
     │    {text: "Hello"}       │                         │
```

---

## Componentes do Servidor

### 1. index.js
Entry point. Cria servidor WebSocket na porta 8080.

### 2. websocket.js
Gerencia mensagens:
- `join` - Cliente entra na sala
- `utterance` - Recebe texto para traduzir
- Broadcast da tradução para todos na mesma sala

### 3. translation.js
Integração com Azure Translator API. Traduz texto entre idiomas.

### 4. room.js
Gerencia salas virtuais. Isola mensagens entre grupos.

---

## Fluxo de Funcionamento

1. **Conexão**: Cliente conecta via WebSocket
2. **Join**: Cliente envia `{type: "join"}` com roomId e idioma
3. **Utterance**: Cliente envia mensagem para traduzir
4. **Tradução**: Servidor chama Azure Translator para cada idioma na sala
5. **Broadcast**: Servidor envia tradução para todos na sala
6. **Desconexão**: Cliente é removido do registro

---

## Sistema de Salas

Clientes na mesma `roomId` recebem mensagens uns dos outros.
Clientes em salas diferentes são completamente isolados.

```javascript
// Estrutura interna
clients = Map {
  WebSocket => {
    clientId: "player-1",
    roomId: "room-abc",
    language: "pt-BR"
  }
}
```

---

## Azure Translator

**Endpoint**: `https://api.cognitive.microsofttranslator.com/translate`

**Request**:
```javascript
POST /translate?api-version=3.0&from=pt-BR&to=en-US
Headers: {
  Ocp-Apim-Subscription-Key: KEY,
  Ocp-Apim-Subscription-Region: REGION
}
Body: [{ Text: "Olá" }]
```

**Response**:
```javascript
[{
  translations: [{ text: "Hello", to: "en-US" }]
}]
```

---

## Tratamento de Erros

- **Formato JSON inválido**: Retorna `{type: "error"}`
- **Falha na tradução**: Retorna texto original
- **Azure offline**: Fallback para texto original
- **Desconexão**: Cliente removido automaticamente

---

## Escalabilidade

**Atual**: Single-thread Node.js
- ~1.000-5.000 conexões simultâneas
- Depende de CPU/RAM do servidor

**Para mais**: Usar cluster com Redis ou multiple instances + load balancer

---

## Segurança

⚠️ **Versão atual é para desenvolvimento/demo**

Para produção adicionar:
- Autenticação JWT
- WSS (WebSocket + TLS)
- Rate limiting
- Validação de input

---

## Idiomas Suportados

Todos os idiomas do Azure Translator (100+):
- pt-BR (Português Brasil)
- en-US (Inglês)
- es-ES (Espanhol)
- fr-FR (Francês)
- de-DE (Alemão)
- ja-JP (Japonês)
- E muitos outros...

Lista completa: https://learn.microsoft.com/azure/ai-services/translator/language-support

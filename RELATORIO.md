# Relatório do Projeto Translation Server

## 📋 Visão Geral

O **Translation Server** é um servidor WebSocket em Node.js que fornece tradução em tempo real para múltiplos clientes em salas virtuais. Utiliza o Azure Translator para traduzir mensagens entre diferentes idiomas automaticamente.

## 🏗️ Arquitetura

### Tecnologias Utilizadas
- **Node.js** com módulos ES6
- **WebSocket (ws)** - Comunicação bidirecional em tempo real
- **Azure Translator API** - Serviço de tradução
- **Axios** - Cliente HTTP para requisições à API do Azure
- **dotenv** - Gerenciamento de variáveis de ambiente

### Estrutura de Arquivos

```
translation-server/
├── package.json           # Dependências e scripts
├── src/
│   ├── index.js          # Servidor principal WebSocket
│   ├── websocket.js      # Handlers de mensagens
│   ├── translation.js    # Integração com Azure Translator
│   └── room.js           # Gerenciamento de salas
└── C#/
    └── Program.cs        # Cliente de exemplo em C#
```

## 🔧 Componentes

### 1. `index.js` - Servidor Principal

**Responsabilidades:**
- Inicializar o servidor WebSocket na porta configurada (padrão: 9000)
- Gerenciar conexões de clientes
- Processar mensagens recebidas (**apenas JSON válido**)
- Manter mapa de clientes conectados com suas metadatas

**Fluxo de Conexão:**
```
Cliente conecta → Servidor aceita → Cliente envia JSON → Servidor valida → Processa mensagem
```

**Validação de Mensagens:**
- ✅ Aceita apenas mensagens em formato JSON válido
- ✅ Rejeita strings simples ou dados não-JSON
- ✅ Valida presença do campo `type` obrigatório
- ❌ Retorna erro se JSON inválido ou sem tipo

**Estrutura de Dados dos Clientes:**
```javascript
Map<WebSocket, {
  clientId: string,
  roomId: string,
  language: string
}>
```

### 2. `websocket.js` - Handlers de Mensagens

**Tipos de Mensagens Suportados:**

#### `join` - Entrar em uma Sala
```json
{
  "type": "join",
  "clientId": "user123",
  "roomId": "sala-portugues",
  "language": "pt-BR"
}
```

**Resposta:**
```json
{
  "type": "joined",
  "clientId": "user123",
  "roomId": "sala-portugues"
}
```

#### `utterance` - Enviar Mensagem
```json
{
  "type": "utterance",
  "utteranceId": "msg-001",
  "speakerId": "user123",
  "roomId": "sala-portugues",
  "language": "pt-BR",
  "text": "Olá, como vai?"
}
```

**Processamento:**
1. Identifica todos os clientes na mesma sala
2. Para cada cliente, verifica se o idioma é diferente do original
3. Se diferente, traduz o texto usando Azure Translator
4. Envia mensagem traduzida para cada cliente

**Resposta (para cada cliente):**
```json
{
  "type": "transcription",
  "utteranceId": "msg-001",
  "speakerId": "user123",
  "roomId": "sala-portugues",
  "originalLanguage": "pt-BR",
  "targetLanguage": "en-US",
  "text": "Hello, how are you?"
}
```

### 3. `translation.js` - Azure Translator Integration

**Função Principal:** `translateText()`

**Parâmetros:**
- `text`: Texto a ser traduzido
- `from`: Idioma de origem (ex: "pt-BR", "en-US")
- `to`: Idioma de destino

**Comportamento:**
- Retorna texto original se `from === to`
- Faz requisição POST para Azure Translator API
- Tratamento de erros com fallback para texto original
- Logging detalhado de requisições e respostas

**Requisitos de Configuração:**
```env
AZURE_TRANSLATOR_ENDPOINT=https://api.cognitive.microsofttranslator.com
AZURE_TRANSLATOR_KEY=sua-chave-aqui
AZURE_TRANSLATOR_REGION=eastus
PORT=9000
```

### 4. `room.js` - Gerenciamento de Salas

**Função:** `getClientsInRoom(clients, roomId)`

**Retorno:**
```javascript
[
  { ws: WebSocket, meta: { clientId, roomId, language } },
  { ws: WebSocket, meta: { clientId, roomId, language } },
  ...
]
```

Retorna array com todos os clientes WebSocket que pertencem a uma sala específica.

## 🔄 Fluxo de Funcionamento

### Cenário Completo: Tradução em Tempo Real

```
1. Cliente A (pt-BR) conecta
   └─> Envia: { type: "join", clientId: "A", roomId: "room1", language: "pt-BR" }
   └─> Recebe: { type: "joined", clientId: "A", roomId: "room1" }

2. Cliente B (en-US) conecta
   └─> Envia: { type: "join", clientId: "B", roomId: "room1", language: "en-US" }
   └─> Recebe: { type: "joined", clientId: "B", roomId: "room1" }

3. Cliente C (es-ES) conecta
   └─> Envia: { type: "join", clientId: "C", roomId: "room1", language: "es-ES" }
   └─> Recebe: { type: "joined", clientId: "C", roomId: "room1" }

4. Cliente A envia mensagem em português
   └─> Envia: { 
         type: "utterance", 
         utteranceId: "msg-123",
         speakerId: "A",
         roomId: "room1",
         language: "pt-BR",
         text: "Olá pessoal!"
       }
   
   └─> Servidor processa:
       ├─> Cliente A recebe (pt-BR → pt-BR, sem tradução):
       │   { type: "transcription", ..., text: "Olá pessoal!" }
       │
       ├─> Cliente B recebe (pt-BR → en-US, com tradução):
       │   { type: "transcription", ..., text: "Hello everyone!" }
       │
       └─> Cliente C recebe (pt-BR → es-ES, com tradução):
           { type: "transcription", ..., text: "¡Hola a todos!" }
```

## 📊 Sistema de Logging

O servidor implementa logging detalhado com timestamps e formatação visual:

```
==============================================================================
✅ [10:30:45] CONEXÃO #1 ACEITA
==============================================================================
📊 Total de clientes conectados: 1

──────────────────────────────────────────────────────────────────────────────
📨 [10:30:46] MENSAGEM RECEBIDA (Conexão #1)
──────────────────────────────────────────────────────────────────────────────
📦 Dados brutos: {"type":"join","clientId":"user1"...
📏 Tamanho: 85 bytes
✅ JSON válido detectado
📋 Tipo de mensagem: join

──────────────────────────────────────────────────────────────────────────────
⚙️  [10:30:46] PROCESSANDO MENSAGEM (Conn #1)
──────────────────────────────────────────────────────────────────────────────
```

## 🛡️ Validação e Tratamento de Erros

### Erros Tratados:

1. **JSON Inválido**
   ```json
   { "type": "error", "message": "Formato inválido. Apenas JSON é aceito." }
   ```

2. **Tipo de Mensagem Ausente**
   ```json
   { "type": "error", "message": "Mensagem sem tipo definido." }
   ```

3. **Tipo Desconhecido**
   ```json
   { "type": "error", "message": "Unknown type" }
   ```

4. **Erro de Tradução**
   - Fallback: usa texto original
   - Continua operação sem interromper

5. **Erro de Conexão WebSocket**
   - Logged no console
   - Cliente removido do mapa

## 🚀 Como Executar

### Instalação

```bash
npm install
```

### Configuração

Criar arquivo `.env` na raiz:
```env
AZURE_TRANSLATOR_ENDPOINT=https://api.cognitive.microsofttranslator.com
AZURE_TRANSLATOR_KEY=sua-chave-aqui
AZURE_TRANSLATOR_REGION=eastus
PORT=9000
```

### Iniciar Servidor

```bash
npm start
```

Servidor inicia em: `ws://localhost:9000`

## 📱 Cliente C# (Exemplo)

O projeto inclui um cliente de exemplo em C# (`C#/Program.cs`) que demonstra:
- Conexão WebSocket
- Envio de mensagens `join` e `utterance`
- Recepção de mensagens traduzidas
- Gerenciamento de eventos

## 🔒 Regras de Negócio

### Formato de Comunicação
- ✅ **OBRIGATÓRIO:** Todas as mensagens devem ser JSON válido
- ✅ **OBRIGATÓRIO:** Campo `type` deve estar presente
- ❌ **PROIBIDO:** Envio de strings simples ou texto plano

### Salas (Rooms)
- Clientes só recebem mensagens da sua própria sala
- Múltiplas salas podem existir simultaneamente
- Cada sala é identificada por `roomId` (string)

### Tradução
- Tradução automática entre diferentes idiomas na mesma sala
- Se idioma origem = idioma destino, texto não é traduzido
- Erros de tradução não interrompem o fluxo (fallback para original)

### Idiomas Suportados
Todos os idiomas suportados pelo Azure Translator:
- `pt-BR` - Português (Brasil)
- `en-US` - Inglês (EUA)
- `es-ES` - Espanhol (Espanha)
- `fr-FR` - Francês
- `de-DE` - Alemão
- `ja-JP` - Japonês
- `zh-CN` - Chinês
- E muitos outros...

## 📈 Melhorias Futuras Sugeridas

1. **Autenticação:** Implementar JWT ou similar
2. **Persistência:** Salvar histórico de mensagens
3. **Rate Limiting:** Limitar mensagens por cliente
4. **Reconexão:** Implementar lógica de reconexão automática
5. **Typing Indicators:** Notificar quando usuário está digitando
6. **Presença:** Lista de usuários online por sala
7. **Testes:** Adicionar testes unitários e de integração
8. **Docker:** Containerizar aplicação

---

**Data do Relatório:** 9 de dezembro de 2025  
**Versão:** 1.0.0  
**Status:** ✅ Operacional

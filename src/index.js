import "dotenv/config";
import { WebSocketServer } from "ws";
import { handleMessage, handleDisconnect } from "./websocket.js";

const PORT = process.env.PORT || 9000;

// Map de conexões -> metadata do cliente
// ws => { clientId, roomId, language }
const clients = new Map();

const wss = new WebSocketServer({ port: PORT });

// Rastrear ID único para cada conexão (para logs)
let connectionCounter = 0;

wss.on("connection", (ws) => {
  const connId = ++connectionCounter;
  const timestamp = new Date().toLocaleTimeString("pt-BR");

  console.log(`\n${"=".repeat(80)}`);
  console.log(`✅ [${timestamp}] CONEXÃO #${connId} ACEITA`);
  console.log(`${"=".repeat(80)}`);
  console.log(`📊 Total de clientes conectados: ${wss.clients.size}`);
  console.log();

  ws.on("message", async (data) => {
    // Log detalhado do que chega pelo websocket
    console.log("\n[DEBUG] Mensagem recebida no WebSocket:", data);
    try {
      const rawData = data.toString();
      const msgTimestamp = new Date().toLocaleTimeString("pt-BR");

      console.log(`\n${"─".repeat(80)}`);
      console.log(
        `📨 [${msgTimestamp}] MENSAGEM RECEBIDA (Conexão #${connId})`
      );
      console.log(`${"─".repeat(80)}`);
      console.log(
        `📦 Dados brutos: ${rawData.substring(0, 100)}${
          rawData.length > 100 ? "..." : ""
        }`
      );
      console.log(`📏 Tamanho: ${rawData.length} bytes`);

      // Parse obrigatório como JSON
      let msg;
      try {
        msg = JSON.parse(rawData);
        console.log(`✅ JSON válido detectado`);
        console.log(`📋 Tipo de mensagem: ${msg.type}`);
        console.log(`📋 Conteúdo:`);
        console.log(JSON.stringify(msg, null, 2));

        if (!msg.type) {
          console.error(`❌ Mensagem sem tipo definido`);
          ws.send(
            JSON.stringify({
              type: "error",
              message: "Mensagem sem tipo definido.",
            })
          );
          return;
        }
      } catch (parseError) {
        console.error(`❌ Erro ao processar JSON: ${parseError.message}`);
        console.log(`📝 Dados recebidos: "${rawData}"`);
        ws.send(
          JSON.stringify({
            type: "error",
            message: "Formato inválido. Apenas JSON é aceito.",
          })
        );
        return;
      }

      console.log(`\n🔀 ROTEANDO PARA HANDLER...`);
      await handleMessage(ws, msg, clients, connId);
    } catch (err) {
      console.error(`\n❌ ERRO ao processar mensagem:`, err);
      console.error(err.stack);
      ws.send(
        JSON.stringify({
          type: "error",
          message: "Error processing message",
        })
      );
    }
  });

  ws.on("close", () => {
    const clientData = clients.get(ws);
    const closeTimestamp = new Date().toLocaleTimeString("pt-BR");
    console.log(`\n${"=".repeat(80)}`);
    console.log(`❌ [${closeTimestamp}] CONEXÃO #${connId} FECHADA`);
    if (clientData) {
      console.log(`   Cliente: ${clientData.clientId}`);
      console.log(`   Sala: ${clientData.roomId}`);
      console.log(`   Idioma: ${clientData.language}`);
    }
    // Corrige para nunca mostrar número negativo
    const remaining = Math.max(0, wss.clients.size - 1);
    console.log(`📊 Clientes restantes: ${remaining}`);
    console.log(`${"=".repeat(80)}\n`);
    handleDisconnect(ws, clients);
  });

  ws.on("error", (err) => {
    const errorTimestamp = new Date().toLocaleTimeString("pt-BR");
    console.error(`\n❌ [${errorTimestamp}] ERRO NA CONEXÃO #${connId}:`);
    console.error(err);
  });
});

const timestamp = new Date().toLocaleTimeString("pt-BR");
console.log(`\n${"=".repeat(80)}`);
console.log(`🚀 [${timestamp}] SERVIDOR WEBSOCKET INICIADO`);
console.log(`${"=".repeat(80)}`);
console.log(`🌐 URL: ws://localhost:${PORT}`);
console.log(`🌐 URL (Rede local): ws://[SEU_IP]:${PORT}`);
console.log(`📊 Porta: ${PORT}`);
console.log(`${"=".repeat(80)}\n`);

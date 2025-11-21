import "dotenv/config";
import { WebSocketServer } from "ws";
import { handleMessage, handleDisconnect } from "./websocket.js";

const PORT = process.env.PORT || 8080;

// Map de conexões -> metadata do cliente
// ws => { clientId, roomId, language }
const clients = new Map();

const wss = new WebSocketServer({ port: PORT });

wss.on("connection", (ws) => {
  console.log("Client connected");

  ws.on("message", async (data) => {
    try {
      const msg = JSON.parse(data.toString());
      await handleMessage(ws, msg, clients);
    } catch (err) {
      console.error("Error handling message:", err);
      ws.send(
        JSON.stringify({
          type: "error",
          message: "Invalid message format",
        })
      );
    }
  });

  ws.on("close", () => {
    handleDisconnect(ws, clients);
  });
});

console.log(`WebSocket server running on ws://localhost:${PORT}`);

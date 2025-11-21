import { translateText } from "./translation.js";

export async function handleMessage(ws, msg, clients) {
  switch (msg.type) {
    case "join":
      return handleJoin(ws, msg, clients);
    case "utterance":
      return handleUtterance(ws, msg, clients);
    default:
      ws.send(JSON.stringify({ type: "error", message: "Unknown type" }));
  }
}

export function handleDisconnect(ws, clients) {
  const meta = clients.get(ws);
  if (meta) {
    console.log(`Client disconnected: ${meta.clientId}`);
  }
  clients.delete(ws);
}

function handleJoin(ws, msg, clients) {
  const { clientId, roomId, language } = msg;
  clients.set(ws, { clientId, roomId, language });

  ws.send(JSON.stringify({ type: "joined", clientId, roomId }));
  console.log(`Client joined: ${clientId} (${language}) in ${roomId}`);
}

async function handleUtterance(ws, msg, clients) {
  const { utteranceId, speakerId, roomId, language, text } = msg;

  for (const [clientWs, meta] of clients.entries()) {
    if (meta.roomId !== roomId) continue;

    const targetLanguage = meta.language;
    const translatedText = await translateText({
      text,
      from: language,
      to: targetLanguage,
    });

    const payload = {
      type: "transcription",
      utteranceId: utteranceId || null,
      speakerId,
      roomId,
      originalLanguage: language,
      targetLanguage,
      text: translatedText,
    };

    clientWs.send(JSON.stringify(payload));
  }
}

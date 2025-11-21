// src/rooms.js

export function getClientsInRoom(clients, roomId) {
  const result = [];
  for (const [ws, meta] of clients.entries()) {
    if (meta.roomId === roomId) {
      result.push({ ws, meta });
    }
  }
  return result;
}

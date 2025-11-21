// test-client.js
import WebSocket from "ws";

const SERVER_URL = "ws://localhost:8080";

function createClient(clientId, roomId, language) {
  return new Promise((resolve, reject) => {
    const ws = new WebSocket(SERVER_URL);

    ws.on("open", () => {
      console.log(`✓ Cliente ${clientId} conectado`);

      // Envia mensagem de join
      ws.send(
        JSON.stringify({
          type: "join",
          clientId,
          roomId,
          language,
        })
      );
    });

    ws.on("message", (data) => {
      const msg = JSON.parse(data.toString());
      console.log(`[${clientId}] Recebeu:`, JSON.stringify(msg, null, 2));
    });

    ws.on("error", (error) => {
      console.error(`✗ Erro no cliente ${clientId}:`, error.message);
      reject(error);
    });

    ws.on("close", () => {
      console.log(`✓ Cliente ${clientId} desconectado`);
    });

    // Aguarda um pouco para garantir que a conexão está estabelecida
    setTimeout(() => resolve(ws), 500);
  });
}

async function runTests() {
  console.log("\n=== INICIANDO TESTES DO SERVIDOR DE TRADUÇÃO ===\n");

  try {
    // Teste 1: Criar 3 clientes na mesma sala com idiomas diferentes
    console.log("📋 Teste 1: Conectando 3 clientes na mesma sala (room-1)");
    const client1 = await createClient("client-pt", "room-1", "pt");
    const client2 = await createClient("client-en", "room-1", "en");
    const client3 = await createClient("client-es", "room-1", "es");

    await new Promise((resolve) => setTimeout(resolve, 1000));

    // Teste 2: Enviar uma mensagem de um cliente
    console.log("\n📋 Teste 2: Cliente PT enviando mensagem");
    client1.send(
      JSON.stringify({
        type: "utterance",
        utteranceId: "utt-001",
        speakerId: "client-pt",
        roomId: "room-1",
        language: "pt",
        text: "Olá, como vocês estão?",
      })
    );

    await new Promise((resolve) => setTimeout(resolve, 1000));

    // Teste 3: Cliente em idioma diferente envia mensagem
    console.log("\n📋 Teste 3: Cliente EN enviando mensagem");
    client2.send(
      JSON.stringify({
        type: "utterance",
        utteranceId: "utt-002",
        speakerId: "client-en",
        roomId: "room-1",
        language: "en",
        text: "Hello everyone, I am fine!",
      })
    );

    await new Promise((resolve) => setTimeout(resolve, 1000));

    // Teste 4: Criar cliente em sala diferente
    console.log("\n📋 Teste 4: Criando cliente em sala diferente (room-2)");
    const client4 = await createClient("client-fr", "room-2", "fr");

    await new Promise((resolve) => setTimeout(resolve, 500));

    // Teste 5: Cliente da room-2 envia mensagem (não deve chegar na room-1)
    console.log(
      "\n📋 Teste 5: Cliente FR na room-2 enviando mensagem (isolamento de salas)"
    );
    client4.send(
      JSON.stringify({
        type: "utterance",
        utteranceId: "utt-003",
        speakerId: "client-fr",
        roomId: "room-2",
        language: "fr",
        text: "Bonjour de la chambre 2!",
      })
    );

    await new Promise((resolve) => setTimeout(resolve, 1000));

    // Teste 6: Testar mensagem inválida
    console.log("\n📋 Teste 6: Enviando mensagem com tipo inválido");
    client1.send(
      JSON.stringify({
        type: "invalid_type",
        data: "test",
      })
    );

    await new Promise((resolve) => setTimeout(resolve, 1000));

    // Teste 7: Desconectar clientes
    console.log("\n📋 Teste 7: Desconectando todos os clientes");
    client1.close();
    client2.close();
    client3.close();
    client4.close();

    await new Promise((resolve) => setTimeout(resolve, 1000));

    console.log("\n=== TESTES CONCLUÍDOS COM SUCESSO ===\n");
  } catch (error) {
    console.error("\n✗ Erro durante os testes:", error);
  }

  process.exit(0);
}

runTests();

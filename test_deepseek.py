import os

api_key = os.environ.get("DEEPSEEK_API_KEY")

if not api_key:
    print("❌ DEEPSEEK_API_KEY no está configurada")
    print("   Ejecuta: $env:DEEPSEEK_API_KEY='tu-key' (PowerShell)")
    exit(1)

print(f"✅ API key configurada: {api_key[:10]}...")

import requests

response = requests.post(
    "https://api.deepseek.com/v1/chat/completions",
    headers={"Authorization": f"Bearer {api_key}"},
    json={
        "model": "deepseek-chat",
        "messages": [{"role": "user", "content": "Di 'Hola Mundo' en español"}],
    },
    timeout=30,
)

if response.status_code == 200:
    data = response.json()
    print(f"✅ Respuesta IA: {data['choices'][0]['message']['content']}")
else:
    print(f"❌ Error: {response.status_code}")
    print(f"   {response.text}")
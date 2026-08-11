#!/usr/bin/env python3
"""Monta os slides embutindo cada captura como data URI — a página fica autocontida."""
import base64, json
from pathlib import Path

RAIZ = Path(__file__).resolve().parent.parent
CAP = RAIZ / "tools" / "capturas"
passos = json.loads((CAP / "passos.json").read_text())

# Regra do legado exercitada em cada passo, para a legenda técnica
regras = {
    "02-matricula-nao-cadastrada.png": "RK-046f5592ef5b",
    "03-senha-invalida.png": "RK-f8293cf9dbb3",
    "04-nivel-insuficiente.png": "RK-8ffd715ce9ad",
    "06-conferencia-aberta.png": "RK-45e526801fea",
    "09-confirmacao-de-requantidade.png": "RK-8233e231d6fb · RK-5960908935ee",
    "10-substituiu-nao-somou.png": "AD-17",
    "11-codigo-nao-cadastrado.png": "RK-798f00f19690",
    "12-codigo-de-outro-fornecedor.png": "RK-732bb9300bad",
    "13-leitura-ambigua.png": "FR-13",
    "14-finalizar-conferencia.png": "RK-fa93a48fbecc · A-10",
    "15-documento-fechado.png": "AD-10 · NFR-10",
    "17-oferta-de-cadastrar.png": "RK-dab7d2033e2e · AD-8",
    "18-somente-leitura.png": "RK-69b41cd017dd",
    "19-cadastro-de-codigos.png": "Épico 3 · FR-28",
    "20-codigo-de-outro-produto.png": "RK-2976e3756f6d",
    "21-codigo-repetido-no-produto.png": "RK-99e9bfdcea75",
    "22-consulta-de-conferencias.png": "FR-26 · AD-15",
    "23-consulta-de-fornecedores.png": "FR-34 · FR-35",
    "24-trilha-de-auditoria.png": "FR-41 · FR-42",
}
roteiros = {
    1: "Entrar no sistema", 5: "Roteiro 1 · Conferir a carga da doca 1",
    16: "Roteiro 3 · Permissão de inclusão", 18: "Roteiro 4 · Documento fechado",
    19: "Épico 3 · Códigos de embalagem e etiqueta", 22: "Épico 4 · Consultas do supervisor",
}

slides = []
for i, p in enumerate(passos, start=1):
    b64 = base64.b64encode((CAP / p["arquivo"]).read_bytes()).decode()
    slides.append({
        "n": i, "titulo": p["titulo"], "legenda": p["legenda"],
        "regra": regras.get(p["arquivo"], ""),
        "secao": roteiros.get(i, ""),
        "img": f"data:image/png;base64,{b64}",
    })

(RAIZ / "tools" / "slides-dados.json").write_text(json.dumps(slides))
print(f"{len(slides)} slides, {sum(len(s['img']) for s in slides)//1024} KB de imagem")

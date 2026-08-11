#!/usr/bin/env bash
# Instala o agente de deploy na máquina da POC. Idempotente — rodar de novo
# apenas atualiza os arquivos.
#
#   scp -i ~/.ssh/mundial-poc -r infra/deploy ubuntu@<ip>:/tmp/
#   ssh -i ~/.ssh/mundial-poc ubuntu@<ip> 'sudo bash /tmp/deploy/instalar-agente.sh'
set -euo pipefail

RAIZ=/opt/mundial
FONTE=$RAIZ/fonte
AQUI=$(cd "$(dirname "$0")" && pwd)

command -v git >/dev/null || { apt-get update -qq && apt-get install -y -qq git; }

# Clone do repositório público. Nenhuma credencial na máquina.
if [ ! -d "$FONTE/.git" ]; then
  rm -rf "$FONTE"
  git clone --depth 1 https://github.com/edgars/poc-mundial "$FONTE"
fi
git -C "$FONTE" config --global --add safe.directory "$FONTE" 2>/dev/null || true

mkdir -p "$RAIZ/publico"
chmod 755 "$RAIZ/publico"

install -m 0755 "$AQUI/deploy.sh" /usr/local/bin/mundial-deploy.sh
install -m 0755 "$AQUI/agente.sh" /usr/local/bin/mundial-agente.sh

cat > /etc/systemd/system/mundial-deploy.service <<'UNIT'
[Unit]
Description=Implanta o commit mais recente de origin/main
After=docker.service network-online.target
Requires=docker.service

[Service]
Type=oneshot
ExecStart=/usr/local/bin/mundial-agente.sh
TimeoutStartSec=1800
StandardOutput=journal
StandardError=journal
UNIT

cat > /etc/systemd/system/mundial-deploy.timer <<'UNIT'
[Unit]
Description=Procura commit novo em origin/main a cada minuto

[Timer]
OnBootSec=2min
OnUnitActiveSec=1min
AccuracySec=10s

[Install]
WantedBy=timers.target
UNIT

systemctl daemon-reload
systemctl enable --now mundial-deploy.timer

echo "agente instalado"
systemctl list-timers mundial-deploy.timer --no-pager | head -3

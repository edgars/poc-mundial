#!/bin/sh
# O frontend lê a URL da API de uma variável de ambiente em tempo de execução
# (spine § derived decisions), injetada aqui no index.html.
sed -i "s|__API_URL__|${API_URL:-http://localhost:5000}|g" /usr/share/nginx/html/index.html
sed -i "s|__TZ_APLICACAO__|${TZ_APLICACAO:-America/Sao_Paulo}|g" /usr/share/nginx/html/index.html

# Telemetria do navegador. O coletor OTLP recusa preflight CORS e exige um bearer que não
# pode viver no JavaScript, então o navegador posta em /otlp na própria origem e é aqui que
# o token entra na requisição. Sem coletor configurado o arquivo sai vazio e /otlp não existe.
if [ "${OTEL_WEB:-false}" = "true" ] && [ -n "${OTEL_EXPORTER_OTLP_ENDPOINT:-}" ]; then
  COLETOR=$(echo "${OTEL_EXPORTER_OTLP_ENDPOINT}" | sed 's|/*$||')
  COLETOR_HOST=$(echo "${COLETOR}" | sed -e 's|^[a-z]*://||' -e 's|[:/].*$||')
  # OTEL_EXPORTER_OTLP_HEADERS segue o padrão do OTEL: "Authorization=Bearer <token>".
  AUTORIZACAO=$(echo "${OTEL_EXPORTER_OTLP_HEADERS:-}" | cut -d= -f2-)

  # O upstream vai numa variável de propósito: com nome literal o nginx resolve o DNS na
  # subida e o container inteiro morre se o coletor estiver fora do ar. Com variável ele
  # resolve por requisição, e uma falha de telemetria não derruba o frontend.
  cat > /etc/nginx/otlp.conf <<EOF
location ~ ^/otlp/(.*)\$ {
    resolver 127.0.0.11 1.1.1.1 valid=30s ipv6=off;
    set \$coletor "${COLETOR}";
    # \$1 já traz o v1/traces do exportador — /otlp/v1/traces vira /v1/traces no coletor.
    proxy_pass \$coletor/\$1;
    proxy_set_header Host ${COLETOR_HOST};
    proxy_set_header Authorization "${AUTORIZACAO}";
    proxy_ssl_server_name on;
    proxy_http_version 1.1;
    proxy_set_header Connection "";
}
EOF
  sed -i "s|__OTEL_WEB__|true|g" /usr/share/nginx/html/index.html
else
  : > /etc/nginx/otlp.conf
  sed -i "s|__OTEL_WEB__|false|g" /usr/share/nginx/html/index.html
fi

exec nginx -g 'daemon off;'

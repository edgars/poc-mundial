#!/bin/sh
# O frontend lê a URL da API de uma variável de ambiente em tempo de execução
# (spine § derived decisions), injetada aqui no index.html.
sed -i "s|__API_URL__|${API_URL:-http://localhost:5000}|g" /usr/share/nginx/html/index.html
sed -i "s|__TZ_APLICACAO__|${TZ_APLICACAO:-America/Sao_Paulo}|g" /usr/share/nginx/html/index.html
exec nginx -g 'daemon off;'

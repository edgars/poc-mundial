FROM node:24-alpine AS build
WORKDIR /web
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:alpine AS web
# service.version da telemetria do navegador, injetada no index.html pelo entrypoint junto com
# a API_URL. O deploy passa o commit; ver src/Dockerfile para o porquê de não vir do .env.
ARG VERSAO=dev
ENV VERSAO=$VERSAO
COPY --from=build /web/dist/mundial-web/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh
EXPOSE 3000
ENTRYPOINT ["/entrypoint.sh"]

# Infraestrutura — POC Mundial · Conferência de Recebimento

Só Terraform. Uma CVM na Tencent Cloud rodando a stack inteira em Docker Compose,
atrás de um proxy reverso com TLS automático. Nada de Ansible: o cloud-init da
própria máquina faz a configuração, e o Terraform é a única ferramenta a instalar.

```
infra/terraform/
  versions.tf                       provider e backend
  variables.tf                      entradas
  main.tf                           rede, security group, chave, CVM
  outputs.tf                        IP, URL, próximos passos
  terraform.tfvars.example          copie para terraform.tfvars
  arquivos/docker-compose.yml       a composição que roda na máquina
  arquivos/subir.sh                 sobe/atualiza a aplicação na máquina
  modelos/cloud-init.yaml.tftpl     preparação da máquina no primeiro boot
  modelos/Caddyfile.tftpl           proxy reverso
```

## Arquitetura da máquina

```
navegador → Caddy :443 ─┬─ /      → web:80    (Angular estático)
                        └─ /api/* → api:8080  (ASP.NET Core)
                                       └── db:1433 (só na rede do compose)
```

Tudo na mesma origem: sem CORS, e um só lugar para o certificado. Aponta-se o
navegador para o domínio, nunca para `http://api:8080` — nome de serviço do
compose só resolve dentro da máquina.

## A restrição de arquitetura, verificada

```
mcr.microsoft.com/mssql/server:2022-latest
  → manifesto único, sem manifest list
  → architecture: amd64 · os: linux
```

A imagem é Linux, mas existe **só para x86_64**. Não há variante `arm64`. Use
família de instância x86 — `S5`, `S6` ou `SA5`. O cloud-init confere `uname -m`
no primeiro boot e aborta com mensagem em `/opt/mundial/ARQUITETURA-INCOMPATIVEL.txt`
se a máquina for ARM, em vez de deixar o banco falhando em silêncio.

## Antes de começar

1. **Região.** O padrão é `sa-saopaulo`. Evite regiões do continente chinês:
   servir um domínio em 80/443 lá exige ICP filing (备案), que leva semanas.
2. **Credenciais.** Crie uma chave de API no console da Tencent e exporte:
   ```bash
   export TENCENTCLOUD_SECRET_ID="AKID..."
   export TENCENTCLOUD_SECRET_KEY="..."
   ```
   Nunca coloque chave em arquivo `.tf` ou `.tfvars` — este repositório é público.
3. **Variáveis.**
   ```bash
   cd infra/terraform
   cp terraform.tfvars.example terraform.tfvars
   curl -s https://ifconfig.me     # o IP que vai em cidr_ssh, com /32
   ```
   Preencha `chave_ssh_publica` e `cidr_ssh`. `dominio` é opcional.

## Passo a passo do deployment

### 1. Criar a máquina

```bash
cd infra/terraform
terraform init
terraform plan      # confira o tipo de instância: precisa ser x86
terraform apply
```

Saem o IP público, a URL e os próximos passos.

### 2. Apontar o DNS — se usar domínio

Crie o registro A com o IP da saída do apply, e espere resolver:

```bash
dig +short poc.seudominio.com.br
```

O Caddy só emite o certificado depois que o nome resolver para esta máquina.
Sem `dominio` configurado, pule este passo: a aplicação responde em HTTP na 80.

### 3. Esperar o cloud-init

```bash
ssh ubuntu@<ip> 'cloud-init status --wait'
ssh ubuntu@<ip> 'sudo docker compose -f /opt/mundial/docker-compose.yml ps'
```

Ao fim disto o SQL Server já está no ar e saudável. A aplicação ainda não —
as imagens não existem até a Story 1.1 ser implementada.

### 4. Publicar as imagens

Compile na sua máquina ou em CI, nunca na CVM — o build do Angular e do .NET
não cabe confortavelmente em 8 GB junto com o SQL Server.

```bash
docker build -t <tcr>/mundial-api:0.1.0        -f src/Mundial.Api/Dockerfile .
docker build -t <tcr>/mundial-migrations:0.1.0 -f src/Mundial.Migrations/Dockerfile .
docker build -t <tcr>/mundial-web:0.1.0        -f web/Dockerfile .
docker push <tcr>/mundial-api:0.1.0
docker push <tcr>/mundial-migrations:0.1.0
docker push <tcr>/mundial-web:0.1.0
```

Espelhe também a imagem do SQL Server no seu TCR — assim a apresentação não
depende do MCR estar acessível naquele momento:

```bash
docker pull mcr.microsoft.com/mssql/server:2022-latest
docker tag  mcr.microsoft.com/mssql/server:2022-latest <tcr>/mssql-server:2022
docker push <tcr>/mssql-server:2022
```

Depois é só apontar `imagem_sqlserver` para o espelho e reaplicar.

### 5. Subir a aplicação

```bash
ssh ubuntu@<ip>
sudo docker login <tcr>          # uma vez por máquina
sudo /opt/mundial/subir.sh
```

O `subir.sh` faz `pull` e `up -d` com o profile `app`: migrations roda uma vez,
a API só sobe depois que o DbUp sair com código 0, o web só depois que a API
ficar saudável.

### 6. Conferir

```bash
sudo docker compose -f /opt/mundial/docker-compose.yml ps
sudo docker compose -f /opt/mundial/docker-compose.yml logs migrations
curl -fsS https://poc.seudominio.com.br/api/health
```

### 7. Snapshot antes de apresentar

Com a massa semeada e o roteiro ensaiado, tire um snapshot do disco. É o plano
B mais rápido se a demonstração corromper dado:

```bash
tccli cbs CreateSnapshot --DiskId <disk-id> --SnapshotName pre-demo
```

## Operação do dia a dia

| Ação | Comando |
| --- | --- |
| Nova versão | `sudo /opt/mundial/subir.sh 0.2.0` |
| Rollback | `sudo /opt/mundial/subir.sh 0.1.0` |
| Reiniciar tudo | `sudo docker compose -f /opt/mundial/docker-compose.yml restart` |
| Ver segredos gerados | `sudo cat /opt/mundial/.env` |
| Derrubar a conta | `terraform destroy` |

`terraform destroy` apaga a máquina e o volume junto. Numa POC isso é o
comportamento certo — o dado é semeado, e conta aberta na nuvem depois da
apresentação é desperdício.

## Segredos

`MSSQL_SA_PASSWORD` e `JWT_ASSINATURA` são gerados **na máquina**, no primeiro
boot, e gravados em `/opt/mundial/.env` com permissão `600`. Não passam pelo
estado do Terraform e não existem em lugar nenhum do repositório.

Para lê-los: `sudo cat /opt/mundial/.env`.

## O que este Terraform não faz

- **Não constrói imagem.** Build e push são seus, ou da CI.
- **Não cria o TCR.** Um registry é recurso de conta, não de POC; criar e
  destruir junto com a máquina apagaria as imagens.
- **Não faz backup.** Fora do escopo da POC por decisão registrada no PRD. Para
  o dia da apresentação, o snapshot do passo 7 basta.
- **Não gerencia DNS.** O registro A é manual. Se o domínio estiver no DNSPod,
  dá para automatizar com `tencentcloud_dnspod_record`.

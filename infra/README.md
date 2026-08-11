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
navegador → Caddy :443 ─┬─ /      → web:3000  (nginx com o Angular compilado)
                        └─ /api/* → api:5000  (ASP.NET Core)
                                       └── db:1433 (só na rede do compose)
```

Tudo na mesma origem: sem CORS, e um só lugar para o certificado. O `API_URL`
que o container web injeta no `index.html` é a URL pública — nunca um nome de
serviço do compose, que só resolve dentro da máquina.

Espelha o `docker-compose.yml` da raiz, com três diferenças: imagens publicadas
em vez de build, sem porta de banco nem de API expostas, e o proxy na frente.

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

### 2. Apontar o DNS

A Tencent **não** dá hostname de teste por instância como a Azure faz com
`*.cloudapp.azure.com` — a CVM entrega só IP. Três caminhos, e o Terraform
escolhe nesta precedência:

| Modo | Quando usar | O que você faz |
| --- | --- | --- |
| `dominio` | demonstração com cliente | cria o registro A e espera resolver |
| `duckdns` | ensaio com HTTPS | preenche `duckdns_subdominio` e `duckdns_token` |
| `sslip` | ensaio rápido, sem cadastro | nada |

**Domínio próprio.** Crie o registro A com o IP da saída do apply:

```bash
dig +short poc.seudominio.com.br
```

O Caddy só emite o certificado depois que o nome resolver para esta máquina.

**DuckDNS.** Cadastro grátis em https://www.duckdns.org, e nada a fazer no DNS:
a máquina registra o próprio IP no boot e um timer systemd renova a cada 30
minutos. O endereço vira `<subdominio>.duckdns.org`, com HTTPS automático.

**sslip.io.** Sem nenhuma variável preenchida, a aplicação responde em
`http://<ip>.sslip.io` — o hostname resolve para o próprio IP, sem cadastro.

⚠️ Este modo serve **HTTP puro de propósito**. `sslip.io` não está na Public
Suffix List, então o Let's Encrypt trata todo o domínio como um só: 50
certificados por semana no mundo inteiro, e o limite costuma estar esgotado.
Tentar TLS aqui é descobrir que falhou na hora da apresentação. `duckdns.org`
está na PSL, e por isso é a opção grátis recomendada quando se quer HTTPS.

Depois do boot, o endereço escolhido fica em `/opt/mundial/endereco.txt`.

### 3. Esperar o cloud-init

```bash
ssh ubuntu@<ip> 'cloud-init status --wait'
ssh ubuntu@<ip> 'sudo docker compose -f /opt/mundial/docker-compose.yml ps'
```

Ao fim disto o SQL Server já está no ar e saudável — SQL Server 2022 de verdade,
o alvo do spine. O `azure-sql-edge` do `.env.example` existe só para
desenvolvimento em Apple Silicon; nesta CVM x86 não é preciso.

A aplicação sobe no passo 5, depois que as imagens estiverem no registry.

### 4. Publicar as imagens

Compile na sua máquina ou em CI, nunca na CVM — o build do Angular e do .NET
não cabe confortavelmente em 8 GB junto com o SQL Server.

O `src/Dockerfile` tem dois alvos (`api` e `migracoes`) e o `web/Dockerfile`
tem um (`web`):

```bash
docker buildx build --platform linux/amd64 --target api       -t <tcr>/mundial-api:0.1.0       --push ./src
docker buildx build --platform linux/amd64 --target migracoes -t <tcr>/mundial-migracoes:0.1.0 --push ./src
docker buildx build --platform linux/amd64 --target web       -t <tcr>/mundial-web:0.1.0       --push ./web
```

⚠️ **`--platform linux/amd64` não é opcional se você compila em Mac com Apple
Silicon.** Sem a flag, o build produz imagens arm64 que não rodam na CVM x86, e
o erro só aparece no `docker compose up` lá, com "exec format error".

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

O `subir.sh` faz `pull` e `up -d` com o profile `app`: `migracoes` roda uma vez,
a API só sobe depois que o DbUp sair com código 0, e o web só depois que a API
responder na porta.

### 6. Conferir

```bash
sudo docker compose -f /opt/mundial/docker-compose.yml ps
sudo docker compose -f /opt/mundial/docker-compose.yml logs migracoes
curl -fsS "$(cat /opt/mundial/url-publica.txt)/api/saude"
```

A resposta esperada é `{"estado":"no ar","modoDemo":true}`.

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

`SA_PASSWORD` e a `CONNECTION_STRING` que a carrega são gerados **na máquina**,
no primeiro boot, e gravados em `/opt/mundial/.env` com permissão `600`. Não
passam pelo estado do Terraform e não existem em lugar nenhum do repositório.

O resto do `.env` espelha o `.env.example` da raiz, com `URL_PUBLICA` e
`ORIGEM_WEB` preenchidos com o endereço que o boot resolveu.

Para lê-los: `sudo cat /opt/mundial/.env`.

## O que este Terraform não faz

- **Não constrói imagem.** Build e push são seus, ou da CI.
- **Não cria o TCR.** Um registry é recurso de conta, não de POC; criar e
  destruir junto com a máquina apagaria as imagens.
- **Não faz backup.** Fora do escopo da POC por decisão registrada no PRD. Para
  o dia da apresentação, o snapshot do passo 7 basta.
- **Não registra domínio.** Registro A de domínio próprio é manual; o DuckDNS e
  o sslip.io são automáticos. Se o domínio estiver no DNSPod (da própria
  Tencent), dá para automatizar o registro A com `tencentcloud_dnspod_record`.

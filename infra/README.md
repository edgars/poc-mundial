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
  arquivos/subir.sh                 subida manual, para quando o agente está parado
  modelos/cloud-init.yaml.tftpl     preparação da máquina no primeiro boot
  modelos/Caddyfile.tftpl           proxy reverso
infra/deploy/
  deploy.sh                         build, troca, health check, rollback
  agente.sh                         compara SHAs e chama o deploy do commit alvo
  instalar-agente.sh                instala o agente e o timer (idempotente)
```

A entrega contínua vive em `.github/workflows/deploy.yml` e nos scripts de
`infra/deploy/`. Veja [Entrega contínua](#entrega-contínua).

## Arquitetura da máquina

```
navegador → Caddy :443 ─┬─ /            → web:3000  (nginx com o Angular compilado)
                        ├─ /api/*       → api:5000  (ASP.NET Core)
                        │                    └── db:1433 (só na rede do compose)
                        └─ /deploy.json → arquivo estático com o commit no ar
```

Tudo na mesma origem: sem CORS, e um só lugar para o certificado. O `API_URL`
que o container web injeta no `index.html` é a URL pública — nunca um nome de
serviço do compose, que só resolve dentro da máquina.

Espelha o `docker-compose.yml` da raiz, com três diferenças: consome imagens já
construídas em vez de declarar `build`, não expõe porta de banco nem de API, e
põe o proxy na frente. Quem constrói as imagens é o agente de deploy.

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

A aplicação sobe no passo 4, quando o agente de deploy entra em cena.

### 4. Instalar o agente de deploy

O agente clona o repositório público na máquina, compila as imagens ali e passa
a acompanhar `origin/main`. A partir daqui, **todo push na main vira deploy**.

```bash
scp -i ~/.ssh/<sua-chave> -r infra/deploy ubuntu@<ip>:/tmp/
ssh -i ~/.ssh/<sua-chave> ubuntu@<ip> 'sudo bash /tmp/deploy/instalar-agente.sh'
```

O script é idempotente: rodar de novo apenas atualiza os arquivos. Ele instala
um timer systemd que roda a cada minuto, e nenhuma credencial fica na máquina —
o repositório é público e o clone é anônimo.

O primeiro deploy leva uns 15 minutos (imagem do SDK .NET, `npm ci`, compilação
do Angular, tudo com cache frio). Os seguintes levam cerca de um minuto.

### 5. Conferir

```bash
curl -fsS https://<seu-dominio>/deploy.json     # qual commit está no ar
curl -fsS https://<seu-dominio>/api/saude       # {"estado":"no ar","modoDemo":true}

ssh ubuntu@<ip> 'sudo docker compose -f /opt/mundial/docker-compose.yml --profile app ps'
ssh ubuntu@<ip> 'sudo journalctl -u mundial-deploy.service -n 50'
```

### Sobre arquitetura, para não confundir

O build acontece **na própria CVM, que é x86_64**, então as imagens saem
`linux/amd64` sem nenhuma flag. `--platform` não aparece em lugar nenhum deste
processo.

A flag só importaria em dois cenários que não são o atual:

1. **Compilar no seu Mac com Apple Silicon** e enviar a imagem para a máquina.
   Sem `--platform linux/amd64` o build produz arm64, e o erro só aparece lá,
   como `exec format error`.
2. **Compilar num runner** para publicar num registry — o runner do GitHub já é
   amd64, então seria só uma garantia explícita.

E a CVM é x86 por um motivo só: o **SQL Server 2022 existe apenas para
`linux/amd64`** (manifesto único, sem manifest list). A aplicação em si roda em
arm64 sem problema — .NET, Angular e nginx publicam as duas arquiteturas. É o
banco que amarra, e trocá-lo contrariaria o alvo declarado no spine.

### 6. Snapshot antes de apresentar

Com a massa semeada e o roteiro ensaiado, tire um snapshot do disco. É o plano
B mais rápido se a demonstração corromper dado:

```bash
tccli cbs CreateSnapshot --DiskId <disk-id> --SnapshotName pre-demo
```

## Entrega contínua

Push na `main` → a POC atualiza sozinha. O modelo é **pull**: o timer systemd
na máquina compara `origin/main` com o commit implantado a cada minuto e, quando
difere, roda o `deploy.sh` **daquele commit**.

```
push na main
   ↓ (até 1 min)
agente detecta SHA novo
   ↓
build das 3 imagens · tag = commit · a anterior vira :anterior
   ↓
up -d de migracoes, api e web        ← o db nunca é recriado
   ↓
health check em /api/saude
   ├── ok       → publica /deploy.json com situacao "ok"
   └── falhou   → retag :anterior, sobe de novo, situacao "revertido"
```

Em paralelo, `.github/workflows/deploy.yml` roda os testes e depois espera a POC
publicar aquele commit em `/deploy.json`. O check fica verde quando a aplicação
no ar confirma o commit, e vermelho se não confirmar em 20 minutos ou se tiver
revertido.

**Por que pull e não SSH a partir do runner:** o security group libera a porta 22
para um único IP. Empurrar do GitHub exigiria abrir SSH para as faixas dos
runners — milhares de endereços que mudam — e guardar uma chave privada nos
segredos de um repositório público. O modelo pull dispensa as duas coisas:
nenhum segredo no GitHub, nenhuma porta nova aberta.

## Operação do dia a dia

| Ação | Comando |
| --- | --- |
| Nova versão | `git push origin main` — não há passo manual |
| Ver o que está no ar | `curl https://<dominio>/deploy.json` |
| Forçar deploy agora | `ssh … 'sudo systemctl start mundial-deploy.service'` |
| Implantar um commit específico | `ssh … 'sudo /usr/local/bin/mundial-deploy.sh <sha>'` |
| Log do último deploy | `ssh … 'sudo journalctl -u mundial-deploy.service -n 50'` |
| Pausar a entrega contínua | `ssh … 'sudo systemctl stop mundial-deploy.timer'` |
| Ver segredos gerados | `ssh … 'sudo cat /opt/mundial/.env'` |
| Derrubar a conta | `terraform destroy` |

O rollback é automático quando o health check falha. Para voltar de propósito a
uma versão que passou no health check, rode o `mundial-deploy.sh` com o SHA
desejado — mas lembre que o agente vai reimplantar o topo de `origin/main` no
minuto seguinte. Para segurar, pare o timer antes.

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

- **Não constrói imagem.** Quem constrói é o agente de deploy, na própria
  máquina, a partir do repositório público. Não há registry envolvido — o que
  troca esse desenho é o build passar para o runner do GitHub, publicando no
  GHCR. Vale quando a CVM deixar de ser a máquina de build; hoje o build com
  cache leva um minuto, e puxar ~800 MB de imagem por 20 Mbps levaria mais.
- **Não instala o agente de deploy.** É um passo à parte, do passo 4, porque
  mudar o cloud-init recria a máquina.
- **Não faz backup.** Fora do escopo da POC por decisão registrada no PRD. Para
  o dia da apresentação, o snapshot do passo 6 basta.
- **Não registra domínio.** Registro A de domínio próprio é manual; o DuckDNS e
  o sslip.io são automáticos. Se o domínio estiver no DNSPod (da própria
  Tencent), dá para automatizar o registro A com `tencentcloud_dnspod_record`.

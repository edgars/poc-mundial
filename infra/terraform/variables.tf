variable "regiao" {
  description = "Região da Tencent Cloud. Evite regiões do continente chinês: servir domínio em 80/443 lá exige ICP filing."
  type        = string
  default     = "sa-saopaulo"
}

variable "zona" {
  description = "Zona de disponibilidade. Vazio usa a primeira zona da região que suporta CVM."
  type        = string
  default     = ""
}

variable "nome" {
  description = "Prefixo aplicado a todos os recursos."
  type        = string
  default     = "mundial-poc"
}

variable "tipo_instancia" {
  description = <<-EOT
    Tipo da CVM. Precisa ser família x86_64 — S5, S6 ou SA5.
    A imagem mcr.microsoft.com/mssql/server:2022-latest é publicada
    apenas para linux/amd64; não existe variante arm64. Numa instância
    ARM o cloud-init aborta antes de subir a stack.
    S5.LARGE8 = 4 vCPU / 8 GB, o mínimo confortável para SQL Server + API + web.
  EOT
  type        = string
  default     = "S5.LARGE8"
}

variable "image_id" {
  description = <<-EOT
    ID da imagem pública a usar. Vazio escolhe a Ubuntu Server 24.04 LTS x86_64
    simples da região, descartando as variantes HCC, UEFI, GRID e TK4.
    Em sa-saopaulo a simples é img-mmytdhbn.
  EOT
  type        = string
  default     = ""
}

variable "tamanho_disco" {
  description = "Tamanho do disco de sistema em GB. Guarda as imagens Docker e o volume do banco."
  type        = number
  default     = 100
}

variable "banda_saida" {
  description = "Banda de saída em Mbps."
  type        = number
  default     = 20
}

variable "chave_ssh_publica" {
  description = "Conteúdo da sua chave pública SSH, ex.: file(\"~/.ssh/id_ed25519.pub\")."
  type        = string
}

variable "cidr_ssh" {
  description = "CIDR autorizado a acessar a porta 22. Use seu IP com /32; 0.0.0.0/0 deixa o SSH aberto ao mundo."
  type        = string

  validation {
    condition     = can(cidrhost(var.cidr_ssh, 0))
    error_message = "cidr_ssh precisa ser um CIDR válido, ex.: 189.0.0.1/32."
  }
}

variable "dominio" {
  description = <<-EOT
    Domínio próprio apontando para a máquina, ex.: poc.mundial.com.br.
    Tem precedência sobre o DuckDNS. Crie o registro A antes de subir o proxy.
  EOT
  type        = string
  default     = ""
}

variable "duckdns_subdominio" {
  description = <<-EOT
    Subdomínio no DuckDNS, sem o sufixo — "mundial-poc" vira mundial-poc.duckdns.org.
    Usado quando não há domínio próprio. A máquina registra o próprio IP no boot.

    Por que DuckDNS e não sslip.io: duckdns.org está na Public Suffix List, então
    o limite de emissão do Let's Encrypt é por subdomínio seu. O sslip.io não está,
    e o limite é compartilhado com o mundo inteiro — costuma estar esgotado.
    Cadastro grátis em https://www.duckdns.org.
  EOT
  type        = string
  default     = ""
}

variable "duckdns_token" {
  description = "Token da sua conta DuckDNS. Vai para o estado do Terraform — mantenha o tfstate fora do git."
  type        = string
  default     = ""
  sensitive   = true
}

variable "registry" {
  description = "Host do registry das imagens da aplicação, ex.: mundial.tencentcloudcr.com/poc. Vazio até as imagens existirem."
  type        = string
  default     = ""
}

variable "tag_imagens" {
  description = "Tag das imagens da aplicação. Evite 'latest' — sem tag versionada não há rollback."
  type        = string
  default     = "0.1.0"
}

variable "imagem_sqlserver" {
  description = "Imagem do SQL Server. Espelhe no seu TCR para não depender do MCR no dia da apresentação."
  type        = string
  default     = "mcr.microsoft.com/mssql/server:2022-latest"
}

variable "modo_demo" {
  description = "Liga o andaime de demonstração (seed, reset, painel de códigos) do projeto Mundial.Demo."
  type        = bool
  default     = true
}

variable "fuso_aplicacao" {
  description = "Fuso de exibição da aplicação (AD-19)."
  type        = string
  default     = "America/Sao_Paulo"
}

variable "otel_endpoint" {
  description = <<-EOT
    Base do coletor OTLP (SigNoz), sem o /v1/... no fim. Vazio desliga a telemetria
    na máquina inteira — API e navegador — e a aplicação sobe igual.
  EOT
  type        = string
  default     = ""
}

variable "otel_token" {
  description = <<-EOT
    Bearer do coletor. Vai para /opt/mundial/.env (chmod 600) e para o estado do
    Terraform — mantenha terraform.tfstate fora do git, como o .gitignore já faz.
  EOT
  type        = string
  default     = ""
  sensitive   = true
}

variable "otel_ambiente" {
  description = "Valor de deployment.environment nos sinais. Separa esta máquina do compose local."
  type        = string
  default     = "poc-vm"
}

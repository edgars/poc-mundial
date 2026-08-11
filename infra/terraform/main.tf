# Infraestrutura da POC — Mundial · Conferência de Recebimento.
#
# Uma CVM x86_64 rodando a stack inteira em Docker Compose, atrás de um proxy
# reverso com TLS automático. Consistente com o Deferred do spine: a POC entrega
# em compose; cloud/K8s é outra decisão.

data "tencentcloud_availability_zones_by_product" "cvm" {
  product = "cvm"
}

data "tencentcloud_images" "ubuntu" {
  image_type    = ["PUBLIC_IMAGE"]
  os_name       = "Ubuntu Server 24.04"
  instance_type = var.tipo_instancia
}

locals {
  zona = var.zona != "" ? var.zona : data.tencentcloud_availability_zones_by_product.cvm.zones[0].name

  prefixo_registry = var.registry != "" ? "${trimsuffix(var.registry, "/")}/" : ""

  imagem_api        = "${local.prefixo_registry}mundial-api:${var.tag_imagens}"
  imagem_web        = "${local.prefixo_registry}mundial-web:${var.tag_imagens}"
  imagem_migrations = "${local.prefixo_registry}mundial-migrations:${var.tag_imagens}"

  # O endereço do Caddy é resolvido na máquina, no boot, porque o modo sslip.io
  # depende do IP público — que a própria máquina descobre pelo metadata.
  # Precedência: domínio próprio > DuckDNS > sslip.io em HTTP puro.
  usa_duckdns = var.dominio == "" && var.duckdns_subdominio != ""

  modo_dns = var.dominio != "" ? "dominio" : (local.usa_duckdns ? "duckdns" : "sslip")

  nota_tls = {
    dominio = "TLS automático via Let's Encrypt. O registro A precisa resolver antes de subir o proxy."
    duckdns = "TLS automático via Let's Encrypt. duckdns.org está na Public Suffix List, então o limite de emissão é do seu subdomínio."
    sslip   = "HTTP puro, sem TLS. sslip.io não está na Public Suffix List e o limite do Let's Encrypt é compartilhado com o mundo — tentaria e falharia."
  }[local.modo_dns]

  caddyfile = templatefile("${path.module}/modelos/Caddyfile.tftpl", {
    endereco = "__ENDERECO__" # substituído no boot pelo cloud-init
    nota_tls = local.nota_tls
  })

  cloud_init = templatefile("${path.module}/modelos/cloud-init.yaml.tftpl", {
    compose_conteudo  = file("${path.module}/arquivos/docker-compose.yml")
    caddy_conteudo    = local.caddyfile
    subir_conteudo    = file("${path.module}/arquivos/subir.sh")
    imagem_sqlserver  = var.imagem_sqlserver
    imagem_api        = local.imagem_api
    imagem_web        = local.imagem_web
    imagem_migrations = local.imagem_migrations
    tag               = var.tag_imagens
    fuso              = var.fuso_aplicacao
    modo_demo         = var.modo_demo ? "true" : "false"
    modo_dns          = local.modo_dns
    dominio           = var.dominio
    duckdns_sub       = var.duckdns_subdominio
    duckdns_token     = var.duckdns_token
  })

  etiquetas = {
    projeto  = "mundial-poc"
    ambiente = "poc"
    gerido   = "terraform"
  }
}

# ---------------------------------------------------------------------------
# Rede
# ---------------------------------------------------------------------------

resource "tencentcloud_vpc" "poc" {
  name       = var.nome
  cidr_block = "10.20.0.0/16"
  tags       = local.etiquetas
}

resource "tencentcloud_subnet" "poc" {
  name              = var.nome
  vpc_id            = tencentcloud_vpc.poc.id
  availability_zone = local.zona
  cidr_block        = "10.20.1.0/24"
  tags              = local.etiquetas
}

resource "tencentcloud_security_group" "poc" {
  name        = var.nome
  description = "POC Mundial — HTTP/HTTPS público, SSH restrito. 1433 nunca sai da máquina."
  tags        = local.etiquetas
}

resource "tencentcloud_security_group_rule_set" "poc" {
  security_group_id = tencentcloud_security_group.poc.id

  ingress {
    action      = "ACCEPT"
    cidr_block  = var.cidr_ssh
    protocol    = "TCP"
    port        = "22"
    description = "SSH — apenas do IP autorizado"
  }

  ingress {
    action      = "ACCEPT"
    cidr_block  = "0.0.0.0/0"
    protocol    = "TCP"
    port        = "80"
    description = "HTTP — redireciona para HTTPS e serve o desafio ACME"
  }

  ingress {
    action      = "ACCEPT"
    cidr_block  = "0.0.0.0/0"
    protocol    = "TCP"
    port        = "443"
    description = "HTTPS — a aplicação"
  }

  # 1433 nunca entra aqui. O SQL Server só é alcançável de dentro do compose.

  egress {
    action      = "ACCEPT"
    cidr_block  = "0.0.0.0/0"
    protocol    = "ALL"
    port        = "ALL"
    description = "Saída liberada — pull de imagem e emissão de certificado"
  }
}

# ---------------------------------------------------------------------------
# Acesso
# ---------------------------------------------------------------------------

resource "tencentcloud_key_pair" "poc" {
  key_name   = replace(var.nome, "-", "_")
  public_key = trimspace(var.chave_ssh_publica)
}

# ---------------------------------------------------------------------------
# Máquina
# ---------------------------------------------------------------------------

resource "tencentcloud_instance" "poc" {
  instance_name = var.nome

  availability_zone = local.zona
  image_id          = data.tencentcloud_images.ubuntu.images[0].image_id
  instance_type     = var.tipo_instancia

  instance_charge_type = "POSTPAID_BY_HOUR"

  system_disk_type = "CLOUD_SSD"
  system_disk_size = var.tamanho_disco

  vpc_id                  = tencentcloud_vpc.poc.id
  subnet_id               = tencentcloud_subnet.poc.id
  orderly_security_groups = [tencentcloud_security_group.poc.id]

  allocate_public_ip         = true
  internet_max_bandwidth_out = var.banda_saida

  key_ids = [tencentcloud_key_pair.poc.id]

  user_data = base64encode(local.cloud_init)

  tags = local.etiquetas
}

# Nota: mudar o cloud-init (compose, Caddyfile, subir.sh) recria a máquina e
# apaga o volume do banco. Numa POC o dado é semeado, então recriar é barato —
# mas se quiser preservar, altere os arquivos em /opt/mundial e rode subir.sh.

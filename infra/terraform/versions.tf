terraform {
  required_version = ">= 1.6"

  required_providers {
    tencentcloud = {
      source  = "tencentcloudstack/tencentcloud"
      version = "~> 1.81"
    }
  }

  # Estado local por padrão. Para trabalhar em mais de uma máquina, descomente
  # o backend COS abaixo e rode `terraform init -migrate-state`.
  #
  # backend "cos" {
  #   region = "sa-saopaulo"
  #   bucket = "tfstate-mundial-1300000000"
  #   prefix = "poc"
  # }
}

provider "tencentcloud" {
  region = var.regiao
  # Credenciais vêm do ambiente:
  #   TENCENTCLOUD_SECRET_ID / TENCENTCLOUD_SECRET_KEY
  # Nunca escreva chave neste arquivo.
}

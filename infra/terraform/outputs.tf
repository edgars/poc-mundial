output "ip_publico" {
  description = "IP público da máquina da POC."
  value       = tencentcloud_instance.poc.public_ip
}

output "ssh" {
  description = "Comando de acesso."
  value       = "ssh ubuntu@${tencentcloud_instance.poc.public_ip}"
}

output "url" {
  description = "Endereço da aplicação depois que subir.sh rodar."
  value = {
    dominio = "https://${var.dominio}"
    duckdns = "https://${var.duckdns_subdominio}.duckdns.org"
    sslip   = "http://${tencentcloud_instance.poc.public_ip}.sslip.io"
  }[local.modo_dns]
}

output "modo_dns" {
  description = "Como o endereço público foi resolvido."
  value       = "${local.modo_dns} — ${local.nota_tls}"
}

output "registro_dns" {
  description = "O que fazer no DNS, se houver algo a fazer."
  value = {
    dominio = "Crie o registro A antes de subir o proxy:  ${var.dominio}  A  ${tencentcloud_instance.poc.public_ip}"
    duckdns = "Nada a fazer: a máquina registra o próprio IP no DuckDNS no boot e renova a cada 30 min."
    sslip   = "Nada a fazer: ${tencentcloud_instance.poc.public_ip}.sslip.io já resolve para o IP."
  }[local.modo_dns]
}

output "proximos_passos" {
  description = "O que fazer depois do apply."
  value       = <<-EOT
    1. Aguarde o cloud-init:   ssh ubuntu@${tencentcloud_instance.poc.public_ip} 'cloud-init status --wait'
    2. Confira o banco:        sudo docker compose -f /opt/mundial/docker-compose.yml ps
    3. Autentique no registry: sudo docker login ${var.registry != "" ? var.registry : "<seu-registry>"}
    4. Suba a aplicação:       sudo /opt/mundial/subir.sh
    5. Leia os segredos:       sudo cat /opt/mundial/.env
  EOT
}

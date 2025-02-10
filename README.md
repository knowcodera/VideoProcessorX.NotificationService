# NotificationService

## 📌 Visão Geral
O **NotificationService** é um serviço de notificação robusto para envio de e-mails e gerenciamento de mensagens assíncronas, desenvolvido com **.NET 8**, utilizando **FluentEmail para envio de e-mails**, **Entity Framework Core para persistência de dados**, e **RabbitMQ para mensageria assíncrona**.

## 🚀 Tecnologias Utilizadas
- **.NET 8** - Plataforma de desenvolvimento
- **Entity Framework Core** - ORM para manipulação de dados
- **ASP.NET Core Web API** - Backend do serviço
- **FluentEmail** - Envio de e-mails transacionais
- **RabbitMQ** - Mensageria para eventos assíncronos
- **Polly** - Resiliência e tratamento de falhas
- **Docker** - Containerização do serviço
- **Kubernetes** - Orquestração de containers (planejado)
- **Terraform** - Infraestrutura como código (planejado)
- **Azure Kubernetes Service (AKS)** - Implantação em nuvem (planejado)

## 📁 Estrutura do Projeto
```
NotificationService/
│── docker-compose.yml
│── Dockerfile
│── src/
│   ├── NotificationService.Application/
│   │   ├── DTOs/  # Data Transfer Objects (DTOs)
│   │   ├── Services/  # Serviços de envio de notificação
│   ├── NotificationService.Domain/
│   │   ├── Entities/  # Modelos de domínio (Notificação, etc.)
│   │   ├── Interfaces/  # Interfaces de repositórios e serviços
│   ├── NotificationService.Infraestructure/
│   │   ├── Data/  # DbContext (EF Core)
│   │   ├── Messaging/  # Consumo de eventos com RabbitMQ
│   │   ├── Email/  # Implementação do FluentEmail
│   ├── NotificationService.Presentation/
│   │   ├── Controllers/  # Controladores da API REST
│   │   ├── Program.cs  # Configuração inicial do serviço
│── tests/
│   ├── NotificationService.IntegrationTests/
│   ├── NotificationService.UnitTests/
│── README.md
```

## ⚙️ Funcionalidades
✔ **Envio de e-mails transacionais** com FluentEmail e SMTP  
✔ **Persistência de notificações** com EF Core  
✔ **Fila de mensagens com RabbitMQ** para entrega assíncrona  
✔ **Resiliência e retries** com Polly  
✔ **Testes unitários e integração** com xUnit  
✔ **Containerização com Docker**  

## 🔧 Configuração e Execução

### 1️⃣ Pré-requisitos
Certifique-se de ter instalado:
- [.NET SDK 8.0](https://dotnet.microsoft.com/en-us/download)
- [Docker](https://www.docker.com/)
- [RabbitMQ](https://www.rabbitmq.com/download.html) (caso queira rodar localmente)

### 3️⃣ Configurar Variáveis de Ambiente
Crie um arquivo **appsettings.json** no diretório `NotificationService.Presentation` com o seguinte conteúdo:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=NotificationServiceDB;User Id=sa;Password=YourPassword;"
  },
  "Smtp": {
    "Host": "smtp.seuprovedor.com",
    "Port": 587,
    "Username": "seu-email@dominio.com",
    "Password": "sua-senha",
    "From": "notificacoes@dominio.com"
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Username": "guest",
    "Password": "guest"
  },
  "Resilience": {
    "RetryCount": 3,
    "RetryBaseDelaySeconds": 2,
    "CircuitBreakerThreshold": 5,
    "CircuitBreakerDurationSeconds": 30
  }
}
```

### 4️⃣ Rodar a Aplicação

#### 🔹 Localmente (Sem Docker)
```bash
dotnet build
dotnet run --project src/NotificationService.Presentation
```

#### 🔹 Com Docker
```bash
docker build -t notificationservice .
docker run -p 8080:8080 -e "ASPNETCORE_ENVIRONMENT=Development" notificationservice
```

#### 🔹 Com Docker-Compose (Banco de dados + RabbitMQ)
```bash
docker-compose up -d
```

## 📌 Endpoints da API

| Método | Rota                 | Descrição                          
|--------|----------------------|----------------------------------
| POST   | `/api/email/test`    | Envia um e-mail de teste         
| GET    | `/api/health`        | Verifica a saúde da aplicação     


## 🤖 **Cobertura de Testes**  

| Pacote                                      | Cobertura de Linhas | Cobertura de Branches |
|---------------------------------------------|---------------------|-----------------------|
| `NotificationService.Application`          | 83.72%              | 100%                  |
| `NotificationService.Domain`               | 92.85%              | 100%                  |
| `NotificationService.Infrastructure`       | 1.72%               | 0%                    |
| `NotificationService.Presentation`         | 0%                  | 0%                    |



## 📜 Licença
Este projeto está sob a licença **MIT**.

---

Feito com ❤️ por Roberto Albano


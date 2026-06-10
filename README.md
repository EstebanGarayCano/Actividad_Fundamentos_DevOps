# Actividad 3 - Laboratorio técnico U2 - Configurar un pipeline CI/CD básico usando Jenkins, GitHub Actions 

Carlos Alfonso Muñoz Agudelo

Esteban Giovanny Garay Cano

Carlos Sebastian Castillo Silva

Maestría en Arquitectura de Software y DevOps

Aplicación web de e-commerce con arquitectura de microservicios, desplegada en Google Kubernetes Engine (GKE) mediante dos pipelines de CI/CD independientes: **GitHub Actions** (Integración Continua) y **Jenkins** (Entrega Continua).

---

## Arquitectura general

```
┌─────────────────────────────────────────────────────────┐
│                     GitHub Repository                    │
│  Frontend (React) · Backend · Productos · EcomifyCustomers │
└───────────────────┬─────────────────────────────────────┘
                    │ push / pull_request
          ┌─────────▼──────────┐
          │   GitHub Actions   │  ← Pipeline CI
          │  (ci.yml)          │
          │  · Checkout        │
          │  · dotnet restore  │
          │  · dotnet build    │
          │  · xUnit tests     │
          └─────────┬──────────┘
                    │ imagen lista
          ┌─────────▼──────────┐
          │      Jenkins       │  ← Pipeline CD
          │  (Jenkinsfile)     │
          │  · Checkout        │
          │  · Docker build    │
          │  · Push DockerHub  │
          │  · Deploy K8s      │
          └─────────┬──────────┘
                    │
          ┌─────────▼──────────┐
          │  Google Kubernetes │
          │  Engine (GKE)      │
          └────────────────────┘
```

---

## Stack tecnológico

| Capa | Tecnología |
|------|-----------|
| Frontend | React + Vite |
| Backend (auth/usuarios) | ASP.NET Core 10 |
| Microservicio Productos | ASP.NET Core 10 |
| Microservicio Customers | ASP.NET Core 10 + EF Core + PostgreSQL |
| Base de datos | PostgreSQL en Supabase |
| Contenedores | Docker |
| Registry | DockerHub (`estebangaraycano/`) |
| Orquestación | Kubernetes (GKE) + Helm |
| CI | GitHub Actions |
| CD | Jenkins |
| Pruebas unitarias | xUnit + Moq |

---

## Estructura del repositorio

```
Actividad3-TrabajoK8S/
├── .github/
│   └── workflows/
│       ├── ci.yml                  ← Pipeline CI (GitHub Actions)
│       └── build-push-deploy.yml   ← Pipeline build + deploy GKE
├── EcomifyCustomers/               ← Microservicio CRUD clientes
│   ├── Controllers/
│   ├── Services/
│   ├── DTOs/
│   ├── Models/
│   ├── Data/
│   ├── Dockerfile
│   └── appsettings.json
├── Backend/                        ← Microservicio autenticación
├── Productos/                      ← Microservicio productos
├── Frontend/                       ← App React
├── tests/
│   └── EcomifyCustomers.Tests/     ← 20 pruebas xUnit
├── helm/                           ← Chart Helm para K8s
├── Jenkinsfile                     ← Pipeline CD (Jenkins)
├── DOCUMENTACION.md
└── README.md
```

---

## Flujo CI/CD

### Pipeline CI — GitHub Actions (`ci.yml`)

**Cuándo se ejecuta:** automáticamente ante cada `push` o `pull_request` a la rama `main`.

**Stages:**

| # | Stage | Comando | Propósito |
|---|-------|---------|-----------|
| 1 | **Checkout** | `actions/checkout@v4` | Clona el repositorio completo |
| 2 | **Setup .NET 10** | `actions/setup-dotnet@v4` | Instala el SDK y dependencias del runtime |
| 3 | **Restore dependencies** | `dotnet restore` | Descarga los paquetes NuGet |
| 4 | **Build** | `dotnet build -c Release` | Compila el código en modo Release |
| 5 | **Run xUnit tests** | `dotnet test` | Ejecuta las 20 pruebas unitarias, genera reporte TRX y cobertura OpenCover |
| 6 | **Upload artifacts** | `actions/upload-artifact@v4` | Guarda los resultados de pruebas descargables desde GitHub |

**Resultado esperado:**
```
Test Run Successful.
Total tests: 20 · Passed: 20
```

### Pipeline CD — Jenkins (`Jenkinsfile`)

**Cuándo se ejecuta:** manualmente o ante eventos configurados en Jenkins.

**Stages:**

| # | Stage | Propósito |
|---|-------|-----------|
| 1 | **Checkout** | Clona el repositorio desde GitHub |
| 2 | **Build Docker image** | Construye la imagen con el Dockerfile multi-etapa |
| 3 | **Push to DockerHub** | Publica la imagen en `estebangaraycano/ecomify-customers` |
| 4 | **Deploy to Kubernetes** | Aplica los manifiestos al cluster GKE |

---

## Microservicio EcomifyCustomers

API REST con CRUD completo sobre la tabla `ecommify_customers` en PostgreSQL (Supabase).

**Base URL local:** `http://localhost:8080`  
**Imagen DockerHub:** `estebangaraycano/ecomify-customers:latest`

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/customers` | Lista todos los clientes |
| GET | `/api/customers/{id}` | Obtiene un cliente por ID |
| POST | `/api/customers` | Crea un nuevo cliente |
| PUT | `/api/customers/{id}` | Actualiza un cliente existente |
| DELETE | `/api/customers/{id}` | Elimina un cliente |

Swagger disponible en: `http://localhost:8080/swagger`

### Ejecutar localmente

```bash
cd EcomifyCustomers
dotnet run
```

### Ejecutar en Docker

```bash
docker build -t ecomify-customers:latest -f EcomifyCustomers/Dockerfile EcomifyCustomers
docker run -d -p 8080:8080 ecomify-customers:latest
```

### Obtener imagen desde DockerHub

```bash
docker pull estebangaraycano/ecomify-customers:latest
docker run -d -p 8080:8080 estebangaraycano/ecomify-customers:latest
```

---

## Pruebas unitarias

**Framework:** xUnit 2.9.3 + Moq 4.20.72  
**Total:** 20 pruebas — 0 dependencias de base de datos (todo mockeado)

| Archivo | Pruebas | Qué valida |
|---------|---------|------------|
| `CustomerAddressTests.cs` | 4 | Modelo de dirección (constructor, nulos, igualdad) |
| `CustomerServiceContractTests.cs` | 6 | Contrato de la interfaz `ICustomerService` |
| `CustomersControllerTests.cs` | 10 | Respuestas HTTP del controlador (200, 201, 204, 404, 409) |

**Ejecutar las pruebas:**

```bash
dotnet test tests/EcomifyCustomers.Tests/ --logger "console;verbosity=normal"
```

---

## Ejecución manual del pipeline CI

Para reproducir localmente lo que ejecuta GitHub Actions:

```bash
# 1. Restaurar dependencias
dotnet restore tests/EcomifyCustomers.Tests/EcomifyCustomers.Tests.csproj

# 2. Compilar
dotnet build tests/EcomifyCustomers.Tests/EcomifyCustomers.Tests.csproj --no-restore -c Release

# 3. Ejecutar pruebas con reporte
dotnet test tests/EcomifyCustomers.Tests/EcomifyCustomers.Tests.csproj \
  --no-build -c Release \
  --logger "trx;LogFileName=results.trx" \
  --logger "console;verbosity=normal" \
  --results-directory ./TestResults
```

---

## Herramientas seleccionadas y justificación

| Herramienta | Rol | Justificación |
|-------------|-----|---------------|
| **GitHub Actions** | CI | Integración nativa con el repositorio GitHub, sin infraestructura adicional, ejecución automática ante cada push/PR |
| **Jenkins** | CD | Mayor control sobre el pipeline de entrega, agnóstico del proveedor cloud, estándar de la industria para CD |
| **DockerHub** | Registry | Gratuito, público, compatible con cualquier cluster Kubernetes sin configuración adicional de autenticación |
| **xUnit** | Testing | Framework nativo del ecosistema .NET, integración directa con `dotnet test` y GitHub Actions |
| **Moq** | Mocking | Permite pruebas unitarias sin base de datos real, desacoplando la lógica de negocio |
| **GKE (Kubernetes)** | Orquestación | Escalabilidad automática, gestión declarativa de despliegues, estándar para microservicios en producción |


> Repositorio: https://github.com/EstebanGarayCano/Actividad_Fundamentos_DevOps


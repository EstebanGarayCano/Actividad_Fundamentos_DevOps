# Documentación técnica — Microservicio EcomifyCustomers

Proyecto: Actividad 3 — TrabajoK8S  
Repositorio: https://github.com/EstebanGarayCano/Actividad_Fundamentos_DevOps  
Framework: ASP.NET Core 10 · Entity Framework Core 10 · PostgreSQL (Supabase)

---

## 1. Creación del Microservicio

### 1.1 Conexión a la base de datos

El microservicio se conecta a una instancia de **PostgreSQL alojada en Supabase**. La cadena de conexión está definida en `EcomifyCustomers/appsettings.json`:

```
Host=db.whebvyxbbwlmtpkmiumn.supabase.co
Port=5432
Database=postgres
Username=postgres
SSL Mode=Require
Trust Server Certificate=true
```

La conexión se registra en `Program.cs` usando **Npgsql** con soporte explícito para el tipo compuesto PostgreSQL `address_type`, que representa la dirección del cliente:

```csharp
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.MapComposite<CustomerAddress>("address_type");
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dataSource));
```

**Tabla destino:** `ecommify_customers`

| Columna              | Tipo PostgreSQL   | Descripción                                      |
|----------------------|-------------------|--------------------------------------------------|
| `customer_id`        | `text` (PK)       | Identificador único del cliente                  |
| `customer_unique_id` | `text`            | ID alternativo o de referencia externa           |
| `customer_address`   | `address_type`    | Tipo compuesto: `zip_code`, `city`, `state`      |

**Desafío técnico con el tipo compuesto:** EF Core no puede mapear columnas de tipo compuesto PostgreSQL directamente. Se implementó una solución basada en dos estrategias:

- **Lectura:** entidad keyless `CustomerFlat` configurada con `ToSqlQuery`, que descompone el tipo compuesto usando la sintaxis SQL `(customer_address).campo`.
- **Escritura:** SQL directo con `ExecuteSqlAsync` y el cast `ROW(zip, city, state)::address_type`.

---

### 1.2 Métodos del servicio

El servicio está definido en la interfaz `ICustomerService` e implementado en `CustomerService`. La interfaz actúa como contrato desacoplado, lo que permite mockear el servicio en las pruebas unitarias sin necesidad de una base de datos real.

| Método                                       | Descripción                                               |
|----------------------------------------------|-----------------------------------------------------------|
| `GetAllAsync()`                              | Retorna todos los clientes en la tabla                    |
| `GetByIdAsync(string id)`                    | Retorna un cliente por su `customer_id`, o `null`         |
| `CreateAsync(CreateCustomerDto dto)`         | Inserta un nuevo registro con dirección compuesta         |
| `UpdateAsync(string id, UpdateCustomerDto dto)` | Actualiza `unique_id` y dirección de un cliente existente |
| `DeleteAsync(string id)`                     | Elimina un cliente; retorna `true` si se afectó alguna fila |

**Lectura — `GetAllAsync` y `GetByIdAsync`:**
```csharp
var rows = await db.CustomersFlat.AsNoTracking().ToListAsync();
return rows.Select(ToDto);
```
La query subyacente descompone el tipo compuesto:
```sql
SELECT customer_id, customer_unique_id,
       (customer_address).zip_code AS zip_code,
       (customer_address).city     AS city,
       (customer_address).state    AS state
FROM ecommify_customers
```

**Escritura — `CreateAsync`:**
```csharp
await db.Database.ExecuteSqlAsync($"""
    INSERT INTO ecommify_customers (customer_id, customer_unique_id, customer_address)
    VALUES (
        {dto.CustomerId},
        {dto.CustomerUniqueId},
        ROW({dto.CustomerAddress!.ZipCode}, {dto.CustomerAddress.City}, {dto.CustomerAddress.State})::address_type
    )
""");
```

---

### 1.3 Endpoints expuestos

Base URL local: `http://localhost:5000` (modo desarrollo)  
Base URL Docker: `http://localhost:8080`

| Método HTTP | Ruta                      | Descripción                       | Códigos de respuesta           |
|-------------|---------------------------|-----------------------------------|--------------------------------|
| `GET`       | `/api/customers`          | Lista completa de clientes        | `200 OK`                       |
| `GET`       | `/api/customers/{id}`     | Cliente por ID                    | `200 OK` · `404 Not Found`     |
| `POST`      | `/api/customers`          | Crear nuevo cliente               | `201 Created` · `409 Conflict` |
| `PUT`       | `/api/customers/{id}`     | Actualizar cliente existente      | `200 OK` · `404 Not Found`     |
| `DELETE`    | `/api/customers/{id}`     | Eliminar cliente                  | `204 No Content` · `404 Not Found` |

**Estructura del cuerpo JSON para `POST /api/customers`:**
```json
{
  "customerId": "cliente-001",
  "customerUniqueId": "uid-abc123",
  "customerAddress": {
    "zipCode": "01310",
    "city": "São Paulo",
    "state": "SP"
  }
}
```

**Estructura del cuerpo JSON para `PUT /api/customers/{id}`:**
```json
{
  "customerUniqueId": "uid-actualizado",
  "customerAddress": {
    "zipCode": "20040",
    "city": "Rio de Janeiro",
    "state": "RJ"
  }
}
```

**Respuesta exitosa de `GET /api/customers/{id}`:**
```json
{
  "customerId": "cliente-001",
  "customerUniqueId": "uid-abc123",
  "customerAddress": {
    "zipCode": "01310",
    "city": "São Paulo",
    "state": "SP"
  }
}
```

La documentación Swagger interactiva está disponible en: `http://localhost:8080/swagger`

---

## 2. Docker

### 2.1 Configuración

El microservicio se empaqueta usando un **Dockerfile multi-etapa** ubicado en `EcomifyCustomers/Dockerfile`:

```dockerfile
# Etapa 1: compilar y publicar
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["EcomifyCustomers.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# Etapa 2: imagen de runtime (más pequeña, sin SDK)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "EcomifyCustomers.dll"]
```

El archivo `.dockerignore` evita copiar carpetas innecesarias al contexto de build:

```
bin/
obj/
*.user
.vs/
```

**Dependencias NuGet incluidas en la imagen final:**

| Paquete                              | Versión   | Propósito                        |
|--------------------------------------|-----------|----------------------------------|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.2 | Driver PostgreSQL para EF Core   |
| `Swashbuckle.AspNetCore`             | 10.2.1    | Swagger / OpenAPI                |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.8  | Herramientas de diseño EF Core   |

---

### 2.2 Rutas de acceso

**Construir la imagen:**
```bash
# Ejecutar desde la carpeta raíz del proyecto
docker build -t ecomify-customers:latest -f EcomifyCustomers/Dockerfile EcomifyCustomers
```

**Ejecutar el contenedor:**
```bash
docker run -d \
  --name ecomify-customers \
  -p 8080:8080 \
  ecomify-customers:latest
```

**Verificar que el contenedor está corriendo:**
```bash
docker ps
docker logs ecomify-customers
```

**URLs disponibles una vez levantado el contenedor:**

| Recurso              | URL                                    |
|----------------------|----------------------------------------|
| API base             | `http://localhost:8080/api/customers`  |
| Swagger UI           | `http://localhost:8080/swagger`        |
| Health (implícito)   | `http://localhost:8080/api/customers` → `200 OK` |

> **Nota sobre conectividad:** Docker Desktop para macOS en algunas versiones enruta el tráfico saliente por IPv6, lo que puede generar errores de conexión a Supabase. La solución aplicada fue habilitar IPv6 en el Docker Engine (`fixed-cidr-v6: 2001:db8:1::/64`) y, si persiste el problema, usar un proxy TCP local que reenvíe el tráfico del contenedor a la dirección IPv4 del host con `host.docker.internal`.

---

### 2.3 Endpoints para probar desde Postman

Configurar en Postman la variable de entorno:

| Variable    | Valor                          |
|-------------|--------------------------------|
| `BASE_URL`  | `http://localhost:8080`        |

**GET — Listar todos los clientes**
```
GET {{BASE_URL}}/api/customers
```
Sin cuerpo. Respuesta esperada: `200 OK` con array de objetos.

**GET — Obtener cliente por ID**
```
GET {{BASE_URL}}/api/customers/f7b1d9a0-1234-5678-abcd-ef1234567890
```
Reemplazar el ID con uno real de la base de datos.

**POST — Crear cliente**
```
POST {{BASE_URL}}/api/customers
Content-Type: application/json

{
  "customerId": "test-postman-001",
  "customerUniqueId": "uid-postman",
  "customerAddress": {
    "zipCode": "01310",
    "city": "São Paulo",
    "state": "SP"
  }
}
```
Respuesta esperada: `201 Created` con el recurso creado y header `Location`.

**PUT — Actualizar cliente**
```
PUT {{BASE_URL}}/api/customers/test-postman-001
Content-Type: application/json

{
  "customerUniqueId": "uid-postman-actualizado",
  "customerAddress": {
    "zipCode": "20040",
    "city": "Rio de Janeiro",
    "state": "RJ"
  }
}
```
Respuesta esperada: `200 OK` con los datos actualizados.

**DELETE — Eliminar cliente**
```
DELETE {{BASE_URL}}/api/customers/test-postman-001
```
Sin cuerpo. Respuesta esperada: `204 No Content`.

---

## 3. Configuración de xUnit

El proyecto de pruebas está ubicado en `tests/EcomifyCustomers.Tests/` y referencia directamente el proyecto del microservicio. Esta estructura permite añadir carpetas adicionales (`tests/EcomifyProductos.Tests/`, `tests/EcomifyBackend.Tests/`, etc.) a medida que se agreguen microservicios.

**Dependencias del proyecto de pruebas (`EcomifyCustomers.Tests.csproj`):**

| Paquete                      | Versión   | Propósito                                  |
|------------------------------|-----------|--------------------------------------------|
| `xunit`                      | 2.9.3     | Framework de pruebas unitarias             |
| `xunit.runner.visualstudio`  | 3.1.4     | Integración con el explorador de pruebas   |
| `Moq`                        | 4.20.72   | Creación de mocks para dependencias        |
| `Microsoft.NET.Test.Sdk`     | 17.14.1   | Plataforma de ejecución de tests           |
| `coverlet.collector`         | 6.0.4     | Recolección de cobertura de código         |

---

### 3.1 Pruebas creadas

El proyecto contiene **20 pruebas unitarias** distribuidas en 3 archivos:

#### `Models/CustomerAddressTests.cs` — 4 pruebas

Validan el comportamiento del record `CustomerAddress`:

| Prueba | Descripción |
|--------|-------------|
| `CustomerAddress_Constructor_SetsAllProperties` | El constructor asigna correctamente `ZipCode`, `City` y `State` |
| `CustomerAddress_AllNullValues_CreatesValidRecord` | El record acepta valores nulos sin lanzar excepción |
| `CustomerAddress_Equality_SameValuesMeansEqual` | Dos records con los mismos valores son iguales (igualdad por valor) |
| `CustomerAddress_Equality_DifferentValuesMeansNotEqual` | Dos records con valores distintos no son iguales |

#### `Services/CustomerServiceContractTests.cs` — 6 pruebas

Verifican el contrato de `ICustomerService` usando un mock de Moq. No requieren base de datos:

| Prueba | Descripción |
|--------|-------------|
| `GetAllAsync_ReturnsEnumerableOfCustomerDto` | El método retorna `IEnumerable<CustomerDto>` |
| `GetByIdAsync_WithValidId_ReturnsCustomerDto` | Con ID válido retorna el DTO correcto |
| `GetByIdAsync_WithInvalidId_ReturnsNull` | Con ID inexistente retorna `null` |
| `CreateAsync_ReturnsCreatedCustomer` | El cliente creado coincide con el DTO recibido |
| `DeleteAsync_WithExistingId_ReturnsTrue` | Eliminar un ID existente retorna `true` |
| `DeleteAsync_WithNonExistingId_ReturnsFalse` | Eliminar un ID inexistente retorna `false` |

#### `Controllers/CustomersControllerTests.cs` — 10 pruebas

Validan el comportamiento HTTP del controlador `CustomersController`. Se inyecta un `Mock<ICustomerService>` en el constructor del controlador, aislando completamente la capa de datos:

| Prueba | Descripción |
|--------|-------------|
| `GetAll_ReturnsOk_WithListOfCustomers` | `GET /api/customers` devuelve `200 OK` con la lista |
| `GetAll_ReturnsOk_WithEmptyList` | `GET /api/customers` devuelve `200 OK` con array vacío |
| `GetById_ExistingId_ReturnsOkWithCustomer` | `GET /api/customers/{id}` devuelve `200 OK` con el cliente |
| `GetById_NonExistingId_ReturnsNotFound` | `GET /api/customers/{id}` devuelve `404 Not Found` |
| `Create_NewCustomer_ReturnsCreated` | `POST /api/customers` devuelve `201 Created` |
| `Create_DuplicateId_ReturnsConflict` | `POST` con ID duplicado devuelve `409 Conflict` |
| `Update_ExistingCustomer_ReturnsOkWithUpdatedData` | `PUT /api/customers/{id}` devuelve `200 OK` con datos actualizados |
| `Update_NonExistingId_ReturnsNotFound` | `PUT` con ID inexistente devuelve `404 Not Found` |
| `Delete_ExistingCustomer_ReturnsNoContent` | `DELETE /api/customers/{id}` devuelve `204 No Content` |
| `Delete_NonExistingId_ReturnsNotFound` | `DELETE` con ID inexistente devuelve `404 Not Found` |

---

### 3.2 Cómo se ejecutan las pruebas

#### Desde la terminal (CLI)

Ejecutar todas las pruebas con salida detallada:
```bash
dotnet test tests/EcomifyCustomers.Tests/ --logger "console;verbosity=normal"
```

Ejecutar con reporte de cobertura:
```bash
dotnet test tests/EcomifyCustomers.Tests/ \
  --logger "trx;LogFileName=results.trx" \
  --results-directory ./TestResults \
  /p:CollectCoverage=true \
  /p:CoverletOutputFormat=opencover \
  /p:CoverletOutput=./TestResults/coverage.xml
```

Resultado esperado:
```
Test Run Successful.
Total tests: 20
     Passed: 20
 Total time: ~0.43 Seconds
```

#### Desde Visual Studio

1. Abrir el menú **Test → Test Explorer** (o `Ctrl+E, T`)
2. Hacer clic en **Run All Tests** (botón de play doble ▶▶)
3. Los resultados aparecen en tiempo real con iconos verde/rojo por prueba

Para ejecutar solo un subconjunto:
- Clic derecho sobre una clase de test → **Run Tests**
- Clic derecho sobre un método de test individual → **Run**

#### Desde Visual Studio Code

Con la extensión **.NET Test Explorer** instalada:
1. Abrir el panel **Testing** en la barra lateral (ícono de frasco)
2. Hacer clic en **Run All Tests**

O desde la terminal integrada:
```bash
dotnet test tests/EcomifyCustomers.Tests/
```

---

## 4. Creación del Pipeline CI

El pipeline de integración continua está definido en `.github/workflows/ci.yml` y se ejecuta automáticamente en **GitHub Actions** ante cada push o pull request a la rama `main`.

**Flujo del pipeline:**

```
push / pull_request a main
         │
         ▼
┌─────────────────────────────────────────┐
│  Job: unit-tests (ubuntu-latest)        │
│                                         │
│  1. Checkout del repositorio            │
│  2. Setup .NET 10                       │
│  3. dotnet restore                      │
│  4. dotnet build -c Release             │
│  5. dotnet test → genera TRX + coverage │
│  6. Upload artifacts (TestResults/)     │
└─────────────────────────────────────────┘
         │ (si unit-tests pasa)
         ▼
┌─────────────────────────────────────────┐
│  Job: sonarqube (condicional)           │
│  Solo si SONAR_HOST_URL está definido   │
│                                         │
│  1. sonarscanner begin                  │
│  2. dotnet build                        │
│  3. dotnet test (coverage para Sonar)   │
│  4. sonarscanner end                    │
└─────────────────────────────────────────┘
```

### 4.1 Comandos para ejecutar el pipeline

#### Ejecución automática (GitHub Actions)

El pipeline se dispara automáticamente al hacer:
```bash
git push origin main
```
O al abrir un Pull Request hacia `main`. No requiere ninguna acción manual adicional.

#### Ejecución manual desde GitHub (workflow_dispatch)

1. Ir a `https://github.com/EstebanGarayCano/Actividad_Fundamentos_DevOps/actions`
2. Seleccionar el workflow **"CI — Unit Tests & Code Quality"**
3. Hacer clic en **"Run workflow"** → **"Run workflow"** (botón verde)

#### Ejecución local equivalente al pipeline (terminal)

Para reproducir localmente lo que hace el pipeline paso a paso:

```bash
# Paso 1 — Restaurar dependencias
dotnet restore tests/EcomifyCustomers.Tests/EcomifyCustomers.Tests.csproj

# Paso 2 — Compilar en modo Release
dotnet build tests/EcomifyCustomers.Tests/EcomifyCustomers.Tests.csproj \
  --no-restore -c Release

# Paso 3 — Ejecutar pruebas con reporte TRX y cobertura
dotnet test tests/EcomifyCustomers.Tests/EcomifyCustomers.Tests.csproj \
  --no-build -c Release \
  --logger "trx;LogFileName=results.trx" \
  --logger "console;verbosity=normal" \
  --results-directory ./TestResults \
  /p:CollectCoverage=true \
  /p:CoverletOutputFormat=opencover \
  /p:CoverletOutput=./TestResults/coverage.xml
```

Los archivos generados en `./TestResults/`:

| Archivo            | Descripción                                               |
|--------------------|-----------------------------------------------------------|
| `results.trx`      | Reporte XML de resultados (compatible con Azure DevOps)   |
| `coverage.xml`     | Reporte de cobertura en formato OpenCover (para SonarQube) |

#### Desde Visual Studio (equivalente al pipeline)

1. **Menú Build → Build Solution** (`Ctrl+Shift+B`) — equivale a `dotnet build`
2. **Menú Test → Run All Tests** — equivale a `dotnet test`
3. Los resultados quedan en la ventana **Test Explorer** y en **Output → Tests**

Para generar cobertura con Visual Studio Enterprise:
- Menú **Test → Analyze Code Coverage for All Tests**
- Se abre la ventana **Code Coverage Results** con el desglose por clase y método

---

## Estructura de archivos relevante

```
Actividad3-TrabajoK8S/
├── .github/
│   └── workflows/
│       ├── ci.yml                          ← Pipeline de tests y calidad
│       └── build-push-deploy.yml           ← Pipeline de build Docker y deploy GKE
│
├── EcomifyCustomers/
│   ├── Controllers/
│   │   └── CustomersController.cs          ← Endpoints REST
│   ├── Data/
│   │   └── AppDbContext.cs                 ← Contexto EF Core + ToSqlQuery
│   ├── DTOs/
│   │   ├── CustomerDto.cs
│   │   ├── CreateCustomerDto.cs
│   │   └── UpdateCustomerDto.cs
│   ├── Models/
│   │   ├── Customer.cs                     ← Entidad EF Core
│   │   └── CustomerAddress.cs              ← Record del tipo compuesto PostgreSQL
│   ├── Services/
│   │   ├── ICustomerService.cs             ← Interfaz (contrato)
│   │   └── CustomerService.cs              ← Implementación con EF Core
│   ├── appsettings.json                    ← Connection string a Supabase
│   ├── Dockerfile                          ← Build multi-etapa
│   ├── .dockerignore
│   └── Program.cs                          ← Configuración DI + Npgsql + Swagger
│
└── tests/
    └── EcomifyCustomers.Tests/
        ├── Controllers/
        │   └── CustomersControllerTests.cs ← 10 pruebas del controlador
        ├── Models/
        │   └── CustomerAddressTests.cs     ← 4 pruebas del modelo
        ├── Services/
        │   └── CustomerServiceContractTests.cs ← 6 pruebas del contrato
        └── EcomifyCustomers.Tests.csproj
```

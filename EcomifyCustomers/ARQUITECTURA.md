# Microservicio EcomifyCustomers — Documentación de Arquitectura

**Asignatura:** DevOps — Trabajo Final  
**Entrega:** Actividad 3 — Trabajo con Kubernetes  
**Autor:** Esteban Garay Cano

---

## 1. Descripción General

En el marco de la actividad final de la materia, desarrollamos un microservicio REST para la gestión de clientes de una plataforma de comercio electrónico denominada **Ecomify**. El servicio expone operaciones CRUD completas sobre la tabla `ecommify_customers`, almacenada en una base de datos **PostgreSQL** gestionada a través de **Supabase**.

El microservicio fue construido sobre **.NET 10** utilizando **ASP.NET Core Web API** y **Entity Framework Core** como capa de acceso a datos, siguiendo una arquitectura limpia orientada a microservicios, coherente con el ecosistema de contenedores y orquestación que se trabaja en el resto de la actividad.

---

## 2. Contexto de la Solución

La tabla `ecommify_customers` forma parte de un modelo de datos de e-commerce que incluye entidades como productos, órdenes, vendedores y geolocalizaciones. Dentro de ese modelo, los clientes presentan un desafío técnico particular: la columna `customer_address` no es un tipo primitivo, sino un **composite type** personalizado de PostgreSQL llamado `address_type`, definido como:

```sql
CREATE TYPE address_type AS (
    zip_code  VARCHAR(20),
    city      VARCHAR(100),
    state     VARCHAR(10)
);
```

Este tipo compuesto no tiene soporte nativo en EF Core, lo que nos obligó a diseñar una estrategia específica de acceso a datos, detallada en la sección 5.

---

## 3. Stack Tecnológico

| Componente | Tecnología |
|---|---|
| Lenguaje | C# 13 / .NET 10 |
| Framework web | ASP.NET Core Web API |
| ORM | Entity Framework Core 10 |
| Driver PostgreSQL | Npgsql 10 |
| Documentación API | Swagger / Swashbuckle |
| Base de datos | PostgreSQL (Supabase) |
| Protocolo | HTTP/REST — JSON |

---

## 4. Estructura del Proyecto

```
EcomifyCustomers/
├── Controllers/
│   └── CustomersController.cs     # Endpoints CRUD REST
├── Data/
│   └── AppDbContext.cs            # DbContext + vista keyless CustomerFlat
├── DTOs/
│   ├── CustomerDto.cs             # Respuesta de la API
│   ├── CreateCustomerDto.cs       # Payload de creación
│   └── UpdateCustomerDto.cs       # Payload de actualización
├── Models/
│   ├── Customer.cs                # Entidad EF Core
│   └── CustomerAddress.cs        # Record del composite type
├── Program.cs                     # Configuración de la aplicación
└── appsettings.json               # Cadena de conexión
```

La separación en capas sigue el principio de responsabilidad única:

- **Models** representa la estructura del dominio.
- **DTOs** desacopla la capa de transporte de la capa de persistencia, evitando exponer directamente la entidad de la base de datos.
- **Data** centraliza toda la lógica de acceso a datos.
- **Controllers** se limita a recibir la petición HTTP, delegar al contexto y devolver la respuesta adecuada.

---

## 5. Decisiones de Arquitectura

### 5.1 Estrategia para el tipo compuesto `address_type`

Entity Framework Core no soporta el mapeo nativo de composite types de PostgreSQL como propiedades de una entidad. Ante esto, evaluamos tres alternativas:

| Alternativa | Descripción | Descartada por |
|---|---|---|
| `HasColumnType("address_type")` con ValueConverter | Convertir el tipo C# a string antes de persistir | Npgsql 10 no acepta lectura de `address_type` como `string` en tiempo de ejecución |
| `MapComposite<T>` en el DataSource de Npgsql | Registro del tipo en el driver ADO | El validador de EF Core no reconoce el mapeo al construir el modelo |
| Vista keyless (`ToSqlQuery`) + raw SQL para escrituras | SQL nativo con descomposición del composite | **Seleccionada** — funciona correctamente en lectura y escritura |

**Solución adoptada:** definimos una entidad auxiliar `CustomerFlat` sin clave (`HasNoKey`) que EF Core materializa a partir de una consulta SQL fija. Esta consulta descompone el composite type usando la sintaxis de PostgreSQL `(customer_address).campo`:

```sql
SELECT customer_id,
       customer_unique_id,
       (customer_address).zip_code AS zip_code,
       (customer_address).city     AS city,
       (customer_address).state    AS state
FROM ecommify_customers
```

Para las operaciones de escritura (INSERT / UPDATE), utilizamos `ExecuteSqlAsync` con parámetros interpolados y el constructor `ROW(...)::address_type` de PostgreSQL:

```sql
INSERT INTO ecommify_customers (customer_id, customer_unique_id, customer_address)
VALUES ({id}, {uniqueId}, ROW({zip}, {city}, {state})::address_type)
```

Este enfoque garantiza seguridad ante inyección SQL (los parámetros son enviados como `@p0`, `@p1`... por EF Core) y preserva la integridad del tipo en la base de datos.

### 5.2 Separación de responsabilidades en el DbContext

El `AppDbContext` expone dos `DbSet`:

- **`Customers`** — entidad completa para operaciones de escritura (INSERT, DELETE, verificación de existencia).
- **`CustomersFlat`** — vista keyless para lecturas, que incluye los campos del composite type descompuestos.

Esta dualidad permite aprovechar EF Core para el seguimiento de cambios en las operaciones simples, y SQL nativo donde el ORM no tiene soporte.

### 5.3 Diseño de la API REST

La API respeta las convenciones REST estándar:

| Método HTTP | Ruta | Acción | Código de respuesta |
|---|---|---|---|
| `GET` | `/api/customers` | Listar todos | `200 OK` |
| `GET` | `/api/customers/{id}` | Obtener por ID | `200 OK` / `404 Not Found` |
| `POST` | `/api/customers` | Crear cliente | `201 Created` / `409 Conflict` |
| `PUT` | `/api/customers/{id}` | Actualizar cliente | `200 OK` / `404 Not Found` |
| `DELETE` | `/api/customers/{id}` | Eliminar cliente | `204 No Content` / `404 Not Found` |

### 5.4 CORS habilitado

Se configuró una política CORS permisiva (`AllowAnyOrigin / Method / Header`) para permitir el consumo del servicio desde cualquier frontend durante la etapa de desarrollo y pruebas, sin restricciones de dominio.

---

## 6. Configuración de la Base de Datos

La conexión se establece mediante la cadena de conexión configurada en `appsettings.json`, con SSL obligatorio (requerido por Supabase):

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=<host>;Port=5432;Database=postgres;Username=postgres;Password=<password>;SSL Mode=Require;Trust Server Certificate=true"
}
```

No se utilizan migraciones de EF Core ya que la tabla y el tipo `address_type` preexisten en la base de datos. El microservicio opera en modo **database-first**: conecta al esquema existente sin alterarlo.

---

## 7. Documentación Interactiva

El servicio expone una interfaz Swagger UI en:

```
http://localhost:5184/swagger
```

Desde esta interfaz es posible ejecutar cada endpoint, inspeccionar los esquemas de request/response y explorar la API sin necesidad de herramientas externas.

---

## 8. Cómo Ejecutar Localmente

```bash
cd EcomifyCustomers
dotnet run
```

El servicio queda disponible en `http://localhost:5184`.

### Ejemplo de payload para POST / PUT

```json
{
  "customerId": "cliente-001",
  "customerUniqueId": "unique-abc123",
  "customerAddress": {
    "zipCode": "01310",
    "city": "São Paulo",
    "state": "SP"
  }
}
```

---

## 9. Integración en el Ecosistema de la Actividad

Este microservicio se integra como un componente más del sistema distribuido definido en la actividad, junto con los servicios de autenticación (`Backend`), productos (`Productos`) y el frontend React. Al igual que los otros servicios, está preparado para ser contenedorizado mediante Docker y desplegado en un clúster de Kubernetes en Google GKE, siguiendo el mismo pipeline de CI/CD definido con GitHub Actions.

---

## 10. Diagrama de Componentes

```
┌─────────────────────────────────────────────────────────┐
│                     Cliente (Frontend)                  │
└──────────────────────────┬──────────────────────────────┘
                           │ HTTP/REST (JSON)
                           ▼
┌─────────────────────────────────────────────────────────┐
│              CustomersController                        │
│  GET /api/customers      GET /api/customers/{id}        │
│  POST /api/customers     PUT /api/customers/{id}        │
│  DELETE /api/customers/{id}                             │
└──────────────────────────┬──────────────────────────────┘
                           │
          ┌────────────────┴────────────────┐
          │                                 │
          ▼                                 ▼
 ┌─────────────────┐              ┌──────────────────────┐
 │  DbSet<Customer>│              │ DbSet<CustomerFlat>  │
 │  (escrituras)   │              │ (lecturas — keyless) │
 └────────┬────────┘              └──────────┬───────────┘
          │                                  │
          └──────────────┬───────────────────┘
                         │
                         ▼
          ┌──────────────────────────┐
          │       AppDbContext       │
          │  Entity Framework Core  │
          │  + raw SQL (address)    │
          └──────────────┬───────────┘
                         │ SSL / TCP 5432
                         ▼
          ┌──────────────────────────┐
          │   PostgreSQL (Supabase)  │
          │  tabla: ecommify_customers│
          │  tipo: address_type      │
          └──────────────────────────┘
```

---

*Documentación generada como parte de la entrega de la Actividad 3 — Maestría en DevOps.*

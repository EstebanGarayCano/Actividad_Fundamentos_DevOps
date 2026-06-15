# Guía de despliegue manual en Google Artifact Registry
## Proyecto: FundamentosDEVOPS — GCP

**Autor:** Esteban Giovanny Garay Cano  
**Proyecto GCP:** `project-54ecee29-e768-42f3-977`  
**Región:** `us-central1`  
**Repositorio:** `devops-repo`

---

## Requisitos previos

Antes de ejecutar cualquier comando, asegúrate de tener instalado:

| Herramienta | Verificar con |
|-------------|---------------|
| Google Cloud SDK | `gcloud version` |
| Docker Desktop | `docker --version` |
| Acceso al proyecto GCP | `gcloud projects describe project-54ecee29-e768-42f3-977` |

---

## Paso 1 — Configurar el proyecto activo en gcloud

Apunta tu CLI de Google Cloud al proyecto correcto:

```bash
gcloud config set project project-54ecee29-e768-42f3-977
```

**Resultado esperado:**
```
Updated property [core/project].
```

Actualiza también las credenciales por defecto para evitar warnings de quota:

```bash
gcloud auth application-default set-quota-project project-54ecee29-e768-42f3-977
```

**Resultado esperado:**
```
Quota project "project-54ecee29-e768-42f3-977" was added to ADC
```

---

## Paso 2 — Habilitar las APIs necesarias

Activa el API de Artifact Registry en el proyecto (solo necesario la primera vez):

```bash
gcloud services enable artifactregistry.googleapis.com \
  --project=project-54ecee29-e768-42f3-977
```

**Resultado esperado:**
```
Operation finished successfully.
```

---

## Paso 3 — Crear el repositorio en Artifact Registry

Crea el repositorio Docker donde se almacenarán las imágenes:

```bash
gcloud artifacts repositories create devops-repo \
  --repository-format=docker \
  --location=us-central1 \
  --description="Microservicios Actividad 3" \
  --project=project-54ecee29-e768-42f3-977
```

**Resultado esperado:**
```
Created repository [devops-repo].
```

> Si el repositorio ya existe, este comando da error. En ese caso omite este paso y continúa con el Paso 4.

---

## Paso 4 — Autenticar Docker contra Artifact Registry

Permite que Docker use tus credenciales de gcloud para subir imágenes:

```bash
gcloud auth configure-docker us-central1-docker.pkg.dev
```

**Resultado esperado:**
```
Adding credentials for: us-central1-docker.pkg.dev
gcloud credential helpers already registered correctly.
```

Este comando solo necesita ejecutarse una vez por máquina.

---

## Imagen 1: EcomifyCustomers

### Paso 5 — Construir la imagen local de EcomifyCustomers

Desde la raíz del repositorio, construye la imagen con el Dockerfile del microservicio:

```bash
docker build \
  -t ecomify-customers:latest \
  -f EcomifyCustomers/Dockerfile \
  EcomifyCustomers
```

**Qué hace cada parte:**
- `-t ecomify-customers:latest` — asigna el nombre y tag a la imagen
- `-f EcomifyCustomers/Dockerfile` — indica la ruta del Dockerfile
- `EcomifyCustomers` — contexto de build (carpeta con el código fuente)

**Resultado esperado:**
```
Successfully built <id>
Successfully tagged ecomify-customers:latest
```

### Paso 6 — Etiquetar la imagen con la ruta de Artifact Registry

Agrega el prefijo del registry para que Docker sepa a dónde enviarla:

```bash
docker tag ecomify-customers:latest \
  us-central1-docker.pkg.dev/project-54ecee29-e768-42f3-977/devops-repo/ecomify-customers:latest
```

**Formato de la ruta:**
```
us-central1-docker.pkg.dev / project-54ecee29-e768-42f3-977 / devops-repo / ecomify-customers:latest
        ↑ región                    ↑ ID del proyecto              ↑ repositorio   ↑ nombre:tag
```

### Paso 7 — Subir la imagen de EcomifyCustomers

```bash
docker push \
  us-central1-docker.pkg.dev/project-54ecee29-e768-42f3-977/devops-repo/ecomify-customers:latest
```

**Resultado esperado:**
```
latest: digest: sha256:a1fe86868bd715c56011d7abab4f46fa0c546d3c410b9448c57181fe5abf1531 size: 856
```

---

## Imagen 2: SonarQube

### Paso 8 — Descargar la imagen oficial de SonarQube

Si no tienes la imagen localmente, descárgala desde Docker Hub:

```bash
docker pull sonarqube:community
```

Si ya la tienes (por haber ejecutado `docker run` anteriormente), omite este paso.

**Verificar que existe localmente:**
```bash
docker images | grep sonarqube
```

### Paso 9 — Etiquetar la imagen de SonarQube con la ruta de Artifact Registry

```bash
docker tag sonarqube:community \
  us-central1-docker.pkg.dev/project-54ecee29-e768-42f3-977/devops-repo/sonarqube:latest
```

### Paso 10 — Subir la imagen de SonarQube

```bash
docker push \
  us-central1-docker.pkg.dev/project-54ecee29-e768-42f3-977/devops-repo/sonarqube:latest
```

**Resultado esperado:**
```
latest: digest: sha256:6740e73e87023935251c55675e9b349be839d60c2584b2c60e2a9d94093770ed size: 2530
```

---

## Verificación final

### Listar las imágenes subidas desde la terminal

```bash
gcloud artifacts docker images list \
  us-central1-docker.pkg.dev/project-54ecee29-e768-42f3-977/devops-repo
```

**Resultado esperado:**
```
IMAGE                                                                              DIGEST       CREATE_TIME
.../devops-repo/ecomify-customers  sha256:a1fe868...  2026-06-12
.../devops-repo/sonarqube          sha256:6740e73...  2026-06-12
```

### Verificar desde la consola de GCP

Accede a la URL del repositorio en el navegador:
```
https://console.cloud.google.com/artifacts/docker/project-54ecee29-e768-42f3-977/us-central1/devops-repo
```

Deberías ver las dos imágenes listadas con su digest y fecha de subida.

---

## Resumen de comandos — secuencia completa

```bash
# 1. Configurar proyecto
gcloud config set project project-54ecee29-e768-42f3-977
gcloud auth application-default set-quota-project project-54ecee29-e768-42f3-977

# 2. Habilitar API (solo primera vez)
gcloud services enable artifactregistry.googleapis.com --project=project-54ecee29-e768-42f3-977

# 3. Crear repositorio (solo primera vez)
gcloud artifacts repositories create devops-repo \
  --repository-format=docker \
  --location=us-central1 \
  --project=project-54ecee29-e768-42f3-977

# 4. Autenticar Docker (solo primera vez por máquina)
gcloud auth configure-docker us-central1-docker.pkg.dev

# 5. Build EcomifyCustomers
docker build -t ecomify-customers:latest -f EcomifyCustomers/Dockerfile EcomifyCustomers

# 6. Tag y push EcomifyCustomers
docker tag ecomify-customers:latest \
  us-central1-docker.pkg.dev/project-54ecee29-e768-42f3-977/devops-repo/ecomify-customers:latest
docker push \
  us-central1-docker.pkg.dev/project-54ecee29-e768-42f3-977/devops-repo/ecomify-customers:latest

# 7. Tag y push SonarQube
docker tag sonarqube:community \
  us-central1-docker.pkg.dev/project-54ecee29-e768-42f3-977/devops-repo/sonarqube:latest
docker push \
  us-central1-docker.pkg.dev/project-54ecee29-e768-42f3-977/devops-repo/sonarqube:latest
```

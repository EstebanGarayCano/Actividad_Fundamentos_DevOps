# Documento técnico — Pipeline CI/CD completo

**Integrante:** 
Carlos Alfonso Muñoz Agudelo
Esteban Giovanny Garay Cano
Carlos Sebastian Castillo Silva
**Asignatura:** Fundamentos de DevOps  

---

## 1. Descripción del flujo CI/CD

El flujo CI/CD implementado automatiza el ciclo de vida de la aplicación desde que se realiza un cambio en el código fuente hasta que la nueva versión queda disponible y monitoreada en el entorno desplegado, para esto se integran GitHub Actions, Jenkins, Docker, Google Artifact Registry, Google Kubernetes Engine, SonarQube/SonarCloud, Snyk, Prometheus y Grafana.

El proceso inicia cuando el desarrollador realiza un cambio en el repositorio de GitHub y lo sube a la rama principal. A partir de ese momento, GitHub Actions ejecuta el workflow de Integración Continua, definido en `ci.yml`, este flujo descarga el código, configura el entorno de .NET, restaura dependencias, compila el proyecto y ejecuta las pruebas unitarias del microservicio `EcomifyCustomers`.

Después de las pruebas, el pipeline ejecuta dos validaciones importantes, primero, Snyk analiza las dependencias del proyecto para identificar posibles vulnerabilidades conocidas en paquetes o librerías utilizadas, luego, SonarQube/SonarCloud realiza el análisis estático del código para revisar aspectos de calidad, mantenibilidad, deuda técnica y posibles riesgos de seguridad.

Cuando las validaciones son exitosas, el workflow de build y despliegue construye las imágenes Docker de los microservicios y las publica en Google Artifact Registry. Posteriormente, el pipeline obtiene acceso al clúster de Google Kubernetes Engine y actualiza los deployments, permitiendo que los pods descarguen las versiones más recientes de las imágenes.

Jenkins complementa este proceso mediante un `Jenkinsfile` versionado en el repositorio. Este archivo permite definir el pipeline como código y ejecutar etapas como checkout del repositorio, construcción de la imagen Docker, autenticación contra el registro de imágenes y publicación de la imagen. De esta manera, Jenkins funciona como una alternativa controlada para la entrega continua del microservicio.

Prometheus y Grafana se integran como herramientas de observabilidad. Prometheus recolecta métricas de los servicios desplegados en Kubernetes, mientras que Grafana permite visualizar esas métricas en dashboards, esto facilita revisar el estado de los pods, consumo de CPU, uso de memoria y disponibilidad de los servicios.

---

## 2. Herramientas utilizadas y justificación

| Herramienta | Uso dentro del proyecto | Justificación |
|---|---|---|
| GitHub | Repositorio del código fuente | Permite centralizar el proyecto, controlar versiones y activar los workflows automáticamente. |
| GitHub Actions | Integración Continua y despliegue automatizado | Se integra directamente con el repositorio y permite ejecutar pruebas, análisis de seguridad, construcción de imágenes y despliegue. |
| Jenkins | Entrega Continua mediante Jenkinsfile | Permite definir un pipeline como código y ejecutar de forma controlada las etapas de construcción y publicación de imágenes. |
| Docker | Construcción de imágenes de los microservicios | Empaqueta la aplicación y sus dependencias para que pueda ejecutarse de forma consistente en diferentes ambientes. |
| Google Artifact Registry | Registro de imágenes Docker | Almacena las imágenes construidas para que puedan ser consumidas por Kubernetes/GKE. |
| Google Kubernetes Engine (GKE) | Orquestación de contenedores | Permite desplegar, administrar y actualizar los microservicios dentro de un clúster administrado en la nube. |
| Helm | Gestión de despliegues en Kubernetes | Facilita la reutilización y parametrización de manifiestos para desplegar los servicios. |
| Snyk | Análisis de vulnerabilidades en dependencias | Permite detectar paquetes vulnerables antes de que los cambios lleguen al ambiente desplegado. |
| SonarQube/SonarCloud | Análisis estático de código | Ayuda a revisar calidad, mantenibilidad, deuda técnica y posibles riesgos de seguridad. |
| Prometheus | Recolección de métricas | Permite obtener métricas del comportamiento de los servicios desplegados. |
| Grafana | Visualización de métricas | Presenta dashboards para analizar CPU, memoria, estado de pods y disponibilidad de servicios. |

---

## 3. Evidencia de seguridad y monitoreo

La evidencia de seguridad se obtiene principalmente desde GitHub Actions, donde se ejecuta el workflow `CI — Unit Tests, Security & Code Quality`. En este flujo se observa que los jobs de pruebas unitarias, Snyk y SonarQube/SonarCloud finalizaron correctamente, esto demuestra que el código fue validado antes de avanzar hacia la construcción y el despliegue.

Snyk permitió revisar las dependencias del proyecto `EcomifyCustomers.csproj`, buscando vulnerabilidades conocidas que pudieran afectar la seguridad del microservicio, la ejecución correcta del job confirma que esta revisión se integró dentro del pipeline y que no se identificaron vulnerabilidades críticas que bloquearan el proceso.

SonarQube/SonarCloud permitió realizar análisis estático del código fuente, esta validación ayuda a identificar errores, code smells, deuda técnica y posibles problemas de seguridad, al estar integrado en el pipeline, el análisis se ejecuta de forma automática y permite detectar problemas antes de que los cambios lleguen al ambiente final.

Como parte de las buenas prácticas de seguridad, los tokens y credenciales utilizados por las herramientas no deben almacenarse directamente en el código fuente ni quedar visibles en el documento, estos valores deben manejarse mediante GitHub Secrets, Jenkins Credentials o variables protegidas del entorno.

**Evidencias de seguridad incluidas:**

- Ejecución exitosa del workflow `CI — Unit Tests, Security & Code Quality`.
- Job de pruebas unitarias con xUnit finalizado correctamente.
- Job de Snyk Vulnerability Scan finalizado correctamente.
- Job de SonarQube/SonarCloud Static Analysis finalizado correctamente.
- Uso de secretos como `SNYK_TOKEN`, `SONAR_TOKEN` y credenciales de GCP protegidas en el repositorio.

La evidencia de monitoreo se obtiene mediante Prometheus y Grafana, prometheus fue configurado para recolectar métricas de los servicios desplegados en Kubernetes, incluyendo el endpoint `/metrics` del microservicio. Esto permite validar si los servicios se encuentran activos y si están exponiendo información útil para su supervisión.

Grafana se conecta a Prometheus como fuente de datos y permite construir dashboards para visualizar el comportamiento del sistema, a través de estos paneles se pueden observar métricas como disponibilidad del servicio, uso de CPU, uso de memoria y estado de los pods, esta información facilita detectar problemas después del despliegue y tomar decisiones más rápido.

**Evidencias de monitoreo incluidas:**

- Prometheus recolectando métricas desde los servicios desplegados.
- Target del microservicio en estado activo.
- Endpoint `/metrics` disponible para consulta.
- Dashboard de Grafana con visualización de métricas.
- Métricas relacionadas con CPU, memoria y estado de pods.

---

## 4. Reflexión sobre eficiencia operativa

La implementación del pipeline CI/CD permitió mejorar la eficiencia operativa del proyecto, ya que redujo varias tareas repetitivas que antes debían realizarse de forma manual, antes de automatizar el proceso, cada cambio en el código implicaba ejecutar pruebas, construir imágenes Docker, publicarlas y desplegarlas nuevamente, lo que aumentaba la posibilidad de cometer errores o de tener diferencias entre los ambientes de desarrollo y despliegue.

Con el pipeline implementado, cada cambio sigue un flujo más ordenado y controlado. Primero se validan las pruebas unitarias, luego se revisan aspectos de seguridad y calidad del código, y posteriormente se construyen y despliegan las imágenes en Kubernetes. Esto facilita encontrar errores a tiempo, revisar qué ocurrió en cada ejecución y mantener versiones más consistentes de la aplicación.

Además, la integración con Prometheus y Grafana permite observar el comportamiento del sistema después del despliegue, esto es importante porque el proceso no termina cuando la aplicación se publica, sino que continúa con el seguimiento de su estado en ejecución. Gracias a estas herramientas, el equipo puede detectar problemas de disponibilidad, consumo de recursos o fallos en los servicios con mayor rapidez.

En general, el pipeline no solo automatiza la entrega del software, sino que también mejora la calidad del proceso, al integrar pruebas, análisis de seguridad, despliegue y monitoreo, se reduce la intervención manual y se obtiene mayor confianza sobre cada versión publicada.

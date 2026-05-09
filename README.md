🖥️ 📷 SIMPE - Sistema Integral de Monitoreo de Productividad y Estado de Estaciones
1. 🧩 Descripción corta del proyecto
SIMPE es un sistema integral de monitoreo que permite centralizar la recolección, análisis y visualización de información de estaciones de trabajo en una organización. Su objetivo es proporcionar visibilidad en tiempo real sobre el estado de los dispositivos, el cumplimiento de políticas de seguridad y el comportamiento de la red, permitiendo una gestión proactiva de la infraestructura TI. [SIMPE | Word]

2. 🏗️ Arquitectura del Sistema
SIMPE está basado en una arquitectura cliente-servidor distribuida, compuesta por los siguientes componentes principales:


Agente (Cliente):
Software instalado en cada estación de trabajo.
Encargado de recolectar información de hardware, software, puertos abiertos y cumplimiento de políticas (GPO).
Envía la telemetría al servidor central de forma segura.


Servidor Central:
Recibe, procesa y almacena la información recolectada.
Ejecuta la lógica de correlación de eventos y detección de anomalías.


Base de Datos:
Almacena la información histórica de telemetría, inventario y eventos.
Diseñada para soportar múltiples agentes concurrentes.


Consola Web (Frontend):
Proporciona un dashboard interactivo en tiempo real.
Permite visualizar el estado de la infraestructura, generar alertas y reportes.


Esta arquitectura permite una gestión centralizada con monitoreo continuo, alineada a modelos modernos de seguridad y administración TI. [SIMPE | Word]

3. 🛠️ Tecnologías utilizadas

Lenguajes de programación:
C# (.NET) → Desarrollo del agente
JavaScript / React → Frontend web


Backend & API:
Servicios web para recepción y procesamiento de datos


Base de Datos:
PostgreSQL


Seguridad:
Comunicación cifrada mediante TLS/SSL (AES-256)


Herramientas de desarrollo:
Visual Studio 2022
Visual Studio Code


Estas tecnologías permiten robustez, escalabilidad y compatibilidad con entornos empresariales. [SIMPE | Word]
4. ⚙️ Proceso de instalación
El despliegue de SIMPE se realiza mediante los siguientes pasos:


Instalación del agente:
Cada estación de trabajo debe ejecutar un archivo instalador .exe.
Este ejecutable instala el agente de monitoreo local.


Ejecución del agente:
El agente se ejecuta de manera local en cada equipo.
Comienza automáticamente la recolección de información del sistema.


Conexión con el servidor:
El agente se conecta a través de una IP pública al servidor principal.
La comunicación se realiza de manera segura (cifrada).


Recepción y visualización:
El servidor central recibe la información.
Los datos son procesados y mostrados en la consola web en tiempo real.


Este modelo permite una instalación sencilla y escalable en múltiples estaciones de trabajo dentro de la organización.
5. ⭐ Características destacadas del proyecto

✅ Monitoreo en tiempo real de estaciones de trabajo
✅ Centralización de la información en una única consola
✅ Auditoría de seguridad (puertos abiertos, políticas GPO)
✅ Generación automática de alertas ante incumplimientos
✅ Inventario de hardware y software
✅ Dashboard interactivo con indicadores visuales
✅ Capacidad de correlación de eventos para detectar anomalías
✅ Enfoque en seguridad proactiva y cumplimiento normativo (ISO 27001)
✅ Solución accesible y ligera frente a herramientas empresariales complejas

Estas funcionalidades permiten reducir riesgos, mejorar la visibilidad y optimizar la gestión del área de TI. [SIMPE | Word]

6. 👨‍💻 Autor
Oscar Fernando Goez Barrientos
Estudiante de Tecnología en Sistemas
Institución Universitaria Salazar y Herrera

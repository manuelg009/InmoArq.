# InmoArq — Épica 1: Gestión de Usuarios y Accesos

Plataforma de e-Commerce inmobiliaria y diseño arquitectónico. Este incremento implementa la autenticación, la autorización por roles y permisos, y la trazabilidad de las operaciones.

**Curso:** ISO615 – Desarrollo de Software · **Estudiante:** Miguel Eduardo Frometa Cipion · **Matrícula:** 10161072

## Qué incluye

- **Registro de usuarios** con validaciones y contraseña cifrada (HU-01).
- **Inicio y cierre de sesión**, con bloqueo tras 5 intentos fallidos y sesión que expira por inactividad (HU-02).
- **Asignación de roles y permisos**: el administrador cambia roles, activa o desactiva cuentas, edita la matriz de permisos y crea roles (HU-03).
- **Restricción de funcionalidades según el perfil**: menú, pantallas y acciones protegidas en el servidor (HU-04).
- **Bitácora de accesos** para trazabilidad (HU-05).
- **54 pruebas automatizadas** de acceso autorizado y no autorizado.

## Requisitos

- [.NET SDK 8](https://dotnet.microsoft.com/download) o superior (9 o 10 también sirven).
- Opcional: Visual Studio 2022/2026 con la carga de trabajo "Desarrollo de ASP.NET y web".

No hace falta instalar una base de datos: por defecto usa SQLite y crea el archivo `inmoarq.db` al arrancar.

## Ejecutar

```bash
dotnet run --project src/InmoArq.Web
```

Abre http://localhost:5000. En Visual Studio: abre `InmoArq.sln`, elige `InmoArq.Web` como proyecto de inicio y presiona F5.

### Usuarios de prueba

| Rol | Correo | Contraseña |
|---|---|---|
| Administrador | admin@inmoarq.do | `Admin2026!` |
| Agente inmobiliario | agente@inmoarq.do | `Agente2026!` |
| Arquitecto | arquitecto@inmoarq.do | `Arqui2026!` |
| Cliente | cliente@inmoarq.do | `Cliente2026!` |

Para volver a los datos iniciales, detén la app y borra `src/InmoArq.Web/inmoarq.db`.

### Usar SQL Server en lugar de SQLite

En `src/InmoArq.Web/appsettings.json`:

```json
"BaseDatos": { "Proveedor": "SqlServer", "DatosDemo": true },
"ConnectionStrings": {
  "Default": "Server=(localdb)\\MSSQLLocalDB;Database=InmoArq;Trusted_Connection=True;TrustServerCertificate=True"
}
```

La base y las tablas se crean solas al arrancar.

## Ejecutar las pruebas

```bash
dotnet test
```

En Visual Studio: *Prueba → Ejecutar todas las pruebas*. Cada prueba levanta la aplicación con su propia base de datos temporal.

## Documentación

| Documento | Contenido |
|---|---|
| [docs/historias_usuario.md](docs/historias_usuario.md) | Historias terminadas con identificadores y criterios de aceptación |
| [docs/plan_pruebas.md](docs/plan_pruebas.md) | Casos de prueba, matriz de acceso y resultados |
| [docs/resultados_pruebas.txt](docs/resultados_pruebas.txt) | Salida de la ejecución de las pruebas |
| [docs/arquitectura.md](docs/arquitectura.md) | Estructura, modelo de datos, roles y diseño de seguridad |
| [docs/guion_demo.md](docs/guion_demo.md) | Pasos para la demostración en vivo |
| [docs/evidencia/](docs/evidencia/) | Capturas de pantalla |

# Arquitectura y seguridad — Épica 1

## Tecnología

| Capa | Elección |
|---|---|
| Plataforma | .NET 8 (corre también en .NET 9 y 10) |
| Web | ASP.NET Core MVC con vistas Razor |
| Datos | Entity Framework Core 8 · SQLite por defecto, SQL Server opcional |
| Autenticación | Cookie de ASP.NET Core (`AddCookie`) |
| Autorización | Políticas por permiso (RBAC) con `IAuthorizationHandler` propio |
| Contraseñas | `PasswordHasher<T>` de ASP.NET Core Identity (PBKDF2 con sal) |
| Pruebas | xUnit, contra la aplicación real en un puerto local |

## Estructura

```
InmoArq.sln
src/InmoArq.Web/
├── Program.cs                 Configuración: BD, cookies, políticas, middleware
├── Controllers/
│   ├── CuentaController.cs    HU-01 registro · HU-02 login/logout y bloqueo
│   ├── InicioController.cs    Tablero y perfil
│   ├── AdminController.cs     HU-03 usuarios, roles, permisos · HU-05 bitácora
│   ├── PropiedadesController.cs  Módulo de negocio (ver / publicar)
│   ├── ProyectosController.cs    Módulo de negocio (ver propios / gestionar todos)
│   └── ErrorController.cs     Páginas 400 / 403 / 404
├── Seguridad/
│   ├── Catalogo.cs            Permisos, roles iniciales, módulos y eventos
│   ├── UsuarioActual.cs       Usuario + rol + permisos de la petición (desde la BD)
│   ├── Autorizacion.cs        Requisito, manejador de permisos, respuesta 403 y validador de sesión
│   └── ServicioBitacora.cs    Registro de auditoría
├── Data/
│   ├── AppDbContext.cs        Modelo EF Core
│   └── DbInicializador.cs     Crea la BD y carga datos iniciales
├── Models/                    Entidades y modelos de vista
└── Views/                     Pantallas Razor
tests/InmoArq.Tests/           35 casos xUnit (54 ejecuciones)
```

## Modelo de datos

```
Roles (Id, Nombre único, Descripcion)
Permisos (Id, Codigo único, Descripcion, Modulo)
RolPermisos (RolId, PermisoId)          ← muchos a muchos
Usuarios (Id, Nombre, Email único, PasswordHash, RolId → Roles,
          Activo, IntentosFallidos, BloqueadoHasta, CreadoEn, UltimoAcceso)
Bitacora (Id, Fecha UTC, UsuarioId, Email, Evento, Detalle, Ip)
Propiedades / Proyectos                 ← módulos de negocio para demostrar permisos
```

## Roles y permisos iniciales

| Permiso | Administrador | Agente inmobiliario | Arquitecto | Cliente |
|---|:-:|:-:|:-:|:-:|
| `propiedades.ver` | ✔ | ✔ | ✔ | ✔ |
| `propiedades.gestionar` | ✔ | ✔ | | |
| `proyectos.ver` | ✔ | | ✔ | ✔ (solo los suyos) |
| `proyectos.gestionar` | ✔ | | ✔ | |
| `usuarios.gestionar` | ✔ 🔒 | | | |
| `roles.gestionar` | ✔ 🔒 | | | |
| `bitacora.ver` | ✔ | | | |

🔒 = el Administrador no puede perder estos permisos. El administrador puede cambiar la matriz y crear roles nuevos desde **Roles y permisos**.

## Cómo funciona el control de acceso

1. **Inicio de sesión:** se verifica el hash de la contraseña. Tras 5 fallos seguidos la cuenta se bloquea 15 minutos. El mensaje de error no revela si el correo existe.
2. **Sesión:** cookie `HttpOnly`, `SameSite=Lax`, que expira tras 30 minutos de inactividad. El cierre de sesión solo se acepta por POST.
3. **En cada petición:** `ValidadorSesion` vuelve a leer al usuario de la base de datos. Si fue desactivado o eliminado, la sesión se cierra.
4. **Autorización:** cada acción declara su permiso con `[Authorize(Policy = Permisos.X)]`. `ManejadorPermisos` consulta los permisos actuales del rol, así que un cambio de rol o de permisos aplica de inmediato.
5. **Acceso denegado:** `ManejadorResultadoAutorizacion` responde **403** y registra `ACCESO_DENEGADO` con la ruta y el permiso que faltaba.
6. **Defensa en profundidad:** el menú oculta lo que el rol no puede usar, pero la protección real está en el servidor. Forzar la URL o enviar el formulario a mano también devuelve 403.
7. **CSRF:** `AutoValidateAntiforgeryToken` valida todos los formularios POST.
8. **Redirecciones:** `ReturnUrl` solo acepta rutas internas (`Url.IsLocalUrl`).

## Eventos de la bitácora

`REGISTRO`, `LOGIN_EXITOSO`, `LOGIN_FALLIDO`, `LOGIN_BLOQUEADO`, `LOGIN_RECHAZADO`, `CUENTA_BLOQUEADA`, `LOGOUT`, `ACCESO_DENEGADO`, `CAMBIO_ROL`, `CAMBIO_PERMISOS`, `ROL_CREADO`, `USUARIO_ACTIVADO`, `USUARIO_DESACTIVADO`, `PROPIEDAD_PUBLICADA`, `PROYECTO_CREADO`.

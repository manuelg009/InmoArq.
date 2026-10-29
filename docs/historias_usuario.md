# Historias de usuario — Épica 1: Configuración / Gestión de Usuarios y Accesos

**Épica:** Como administrador de la plataforma, necesito gestionar los usuarios, roles y permisos del sistema para garantizar que cada persona tenga acceso únicamente a las funcionalidades correspondientes a sus responsabilidades dentro de la e-Commerce inmobiliaria y los procesos de diseño arquitectónico.

**Objetivo de negocio:** establecer un mecanismo seguro de autenticación, autorización y administración de usuarios que permita controlar el acceso a los módulos del sistema y asegurar la trazabilidad de las operaciones realizadas.

| ID | Historia | Prioridad | Puntos | Estado | Pruebas |
|---|---|---|---|---|---|
| HU-01 | Registro de usuarios | Alta | 3 | Terminada | CP-01 a CP-07 |
| HU-02 | Inicio y cierre de sesión | Alta | 5 | Terminada | CP-08 a CP-17 |
| HU-03 | Asignación de roles y permisos | Alta | 5 | Terminada | CP-18 a CP-24 |
| HU-04 | Restricción de funcionalidades según el perfil | Alta | 5 | Terminada | CP-25 a CP-31 |
| HU-05 | Trazabilidad de las operaciones | Media | 3 | Terminada | CP-32 a CP-35 |

---

## HU-01 · Registro de usuarios

**Como** visitante de la plataforma, **quiero** crear una cuenta con mi nombre, correo y contraseña, **para** poder acceder a los servicios de la e-Commerce inmobiliaria.

**Criterios de aceptación**

1. **Dado** que completo el formulario con datos válidos, **cuando** lo envío, **entonces** se crea mi cuenta con el rol **Cliente** y me lleva al inicio de sesión con un mensaje de éxito.
2. El correo debe tener un formato válido.
3. No se permiten dos cuentas con el mismo correo (sin importar mayúsculas o minúsculas).
4. La contraseña debe tener al menos 8 caracteres y combinar letras y números; si no cumple, se muestra el motivo.
5. La contraseña y su confirmación deben coincidir.
6. La contraseña se guarda cifrada (hash), nunca en texto plano.

## HU-02 · Inicio y cierre de sesión

**Como** usuario registrado, **quiero** iniciar y cerrar sesión de forma segura, **para** usar la plataforma y proteger mi cuenta cuando termino.

**Criterios de aceptación**

1. **Dado** un correo y una contraseña correctos, **cuando** inicio sesión, **entonces** entro a mi tablero con los módulos de mi rol.
2. Si el correo o la contraseña son incorrectos, se muestra un mensaje genérico que no revela cuál de los dos falló.
3. Después de 5 intentos fallidos seguidos, la cuenta se bloquea 15 minutos.
4. Una cuenta desactivada por el administrador no puede iniciar sesión.
5. Al cerrar sesión se elimina la sesión y las páginas protegidas vuelven a pedir inicio de sesión. El cierre solo se acepta por POST con token CSRF.
6. Si intento abrir una página protegida sin sesión, me lleva al login y, al entrar, me devuelve a la página que pedí (solo rutas internas).
7. La sesión expira tras 30 minutos de inactividad y todos los formularios llevan token CSRF.

## HU-03 · Asignación de roles y permisos

**Como** administrador, **quiero** asignar un rol a cada usuario y definir qué permisos tiene cada rol, **para** controlar quién puede hacer qué en el sistema.

**Criterios de aceptación**

1. Desde **Usuarios**, el administrador cambia el rol de cualquier cuenta y el cambio aplica de inmediato, incluso en sesiones ya abiertas.
2. Desde **Roles y permisos**, el administrador marca o desmarca permisos por rol en una matriz.
3. El administrador puede crear roles nuevos con nombre único.
4. El administrador puede activar o desactivar cuentas; una cuenta desactivada pierde la sesión en su siguiente acción.
5. Reglas de protección: el administrador no puede cambiar su propio rol ni desactivarse; siempre debe quedar al menos un administrador activo; el rol Administrador conserva siempre `usuarios.gestionar` y `roles.gestionar`.

## HU-04 · Restricción de funcionalidades según el perfil

**Como** administrador de la plataforma, **quiero** que cada usuario solo pueda ver y usar las funciones de su rol, **para** proteger la información y las operaciones del negocio.

**Criterios de aceptación**

1. El menú y el tablero muestran solo los módulos que el rol tiene permitidos.
2. Si un usuario abre por URL un módulo no permitido, recibe **403 – Acceso denegado**.
3. Las acciones (formularios POST) se validan en el servidor, no solo se ocultan en pantalla.
4. Un Cliente solo ve sus propios proyectos de diseño; Arquitecto y Administrador ven todos.
5. Se verifica la matriz completa de acceso: 4 roles × 5 módulos.

## HU-05 · Trazabilidad de las operaciones

**Como** administrador, **quiero** consultar un registro de los accesos y cambios de seguridad, **para** saber quién hizo qué y cuándo.

**Criterios de aceptación**

1. Se registran: registro, inicio de sesión exitoso y fallido, bloqueo, cierre de sesión, acceso denegado, cambio de rol, cambio de permisos, creación de rol, activación y desactivación de cuentas.
2. Cada registro guarda fecha y hora, usuario, evento, detalle e IP.
3. Solo los roles con `bitacora.ver` pueden consultar la bitácora, y se puede filtrar por tipo de evento.

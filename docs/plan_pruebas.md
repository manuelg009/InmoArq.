# Plan y resultados de pruebas — Incremento 1

- **Proyecto de pruebas:** `tests/InmoArq.Tests` (xUnit). Cada prueba levanta la aplicación real en un puerto libre con su propia base de datos y la usa como un navegador: guarda cookies, envía el token CSRF y revisa códigos HTTP, pantallas y base de datos.
- **Cómo ejecutarlas:** `dotnet test` desde la carpeta raíz, o *Prueba → Ejecutar todas las pruebas* en Visual Studio.
- **Resultado:** **54 de 54 pruebas superadas** (35 casos; el CP-25 tiene 20 combinaciones). Salida completa en [`resultados_pruebas.txt`](resultados_pruebas.txt).

Tipo: **A** = acceso autorizado · **N** = acceso no autorizado · **V** = validación de datos · **S** = seguridad

## HU-01 · Registro de usuarios — `HU01_RegistroTests`

| ID | Caso | Tipo | Resultado esperado | Resultado |
|---|---|---|---|---|
| CP-01 | Registro con datos válidos | A | Cuenta creada con rol Cliente; redirige a /Cuenta/Login | ✅ Superado |
| CP-02 | Contraseña almacenada | S | Se guarda como hash PBKDF2 con sal, no en texto plano | ✅ Superado |
| CP-03 | Correo ya registrado (otra capitalización) | V | 400: "Ya existe una cuenta registrada con ese correo." | ✅ Superado |
| CP-04 | Contraseña débil ("corta") | V | 400: mínimo 8 caracteres, letras y números | ✅ Superado |
| CP-05 | Confirmación distinta | V | 400: "Las contraseñas no coinciden." | ✅ Superado |
| CP-06 | Correo con formato inválido | V | 400: "Escribe un correo electrónico válido." | ✅ Superado |
| CP-07 | Usuario recién registrado inicia sesión | A | Entra al tablero | ✅ Superado |

## HU-02 · Inicio y cierre de sesión — `HU02_SesionTests`

| ID | Caso | Tipo | Resultado esperado | Resultado |
|---|---|---|---|---|
| CP-08 | Login con credenciales correctas | A | Sesión creada; tablero "Hola, Ana" | ✅ Superado |
| CP-09 | Contraseña incorrecta | N | 401 con mensaje genérico | ✅ Superado |
| CP-10 | Correo inexistente | N | 401 con el mismo mensaje genérico | ✅ Superado |
| CP-11 | 5 intentos fallidos | S | 423 cuenta bloqueada; ni la clave correcta entra | ✅ Superado |
| CP-12 | Cuenta desactivada | N | 403 "Tu cuenta está desactivada" | ✅ Superado |
| CP-13 | Cerrar sesión | A | Sesión eliminada; la página de inicio vuelve a pedir login | ✅ Superado |
| CP-14 | Cerrar sesión por GET | S | 405, solo se acepta POST | ✅ Superado |
| CP-15 | Página protegida sin sesión | N | Redirige al login y, al entrar, vuelve a la página pedida | ✅ Superado |
| CP-16 | `ReturnUrl` hacia un sitio externo | S | Se ignora; va al inicio | ✅ Superado |
| CP-17 | Formulario sin token CSRF | S | 400 | ✅ Superado |

## HU-03 · Asignación de roles y permisos — `HU03_RolesTests`

| ID | Caso | Tipo | Resultado esperado | Resultado |
|---|---|---|---|---|
| CP-18 | Admin sube a un Cliente a Administrador | A | La sesión abierta del usuario pasa de 403 a 200 en Bitácora sin volver a entrar | ✅ Superado |
| CP-19 | Admin quita `propiedades.gestionar` al Agente | A | El Agente deja de ver "Publicar propiedad" | ✅ Superado |
| CP-20 | Intento de quitar todos los permisos al Administrador | S | Conserva `usuarios.gestionar` y `roles.gestionar` | ✅ Superado |
| CP-21 | Admin intenta cambiar su propio rol | S | Rechazado; sigue siendo Administrador | ✅ Superado |
| CP-22 | Admin intenta desactivarse | S | Rechazado; sigue activo | ✅ Superado |
| CP-23 | Admin desactiva a un usuario con sesión abierta | A | El usuario pierde la sesión en su siguiente clic | ✅ Superado |
| CP-24 | Crear rol nuevo y repetido | V | Se crea "Supervisor de obra"; el duplicado se rechaza | ✅ Superado |

## HU-04 · Restricción de funcionalidades según el perfil — `HU04_RestriccionTests`

### CP-25 · Matriz de acceso autorizado / no autorizado (20 casos)

Cada celda es una prueba: se inicia sesión con el rol y se abre la ruta. **200** = acceso autorizado, **403** = acceso denegado.

| Rol | /Propiedades | /Proyectos | /Admin/Usuarios | /Admin/Roles | /Admin/Bitacora |
|---|---|---|---|---|---|
| Administrador | 200 ✅ | 200 ✅ | 200 ✅ | 200 ✅ | 200 ✅ |
| Agente inmobiliario | 200 ✅ | 403 ✅ | 403 ✅ | 403 ✅ | 403 ✅ |
| Arquitecto | 200 ✅ | 200 ✅ | 403 ✅ | 403 ✅ | 403 ✅ |
| Cliente | 200 ✅ | 200 ✅ | 403 ✅ | 403 ✅ | 403 ✅ |

**Resultado:** 20 de 20 superadas: 10 accesos autorizados (200) y 10 no autorizados (403).

| ID | Caso | Tipo | Resultado esperado | Resultado |
|---|---|---|---|---|
| CP-26 | Menú del Cliente | N | Solo muestra Propiedades y Diseño; no muestra Administración | ✅ Superado |
| CP-27 | Cliente publica propiedad forzando la URL (POST) | N | 403 y no se guarda nada | ✅ Superado |
| CP-28 | Agente publica propiedad | A | Se guarda con precio RD$ 18,500,000 | ✅ Superado |
| CP-29 | Cliente abre proyectos | N | Solo ve su proyecto, no los de otros | ✅ Superado |
| CP-30 | Arquitecto abre proyectos | A | Ve todos los proyectos | ✅ Superado |
| CP-31 | Agente intenta darse rol Administrador (POST) | N | 403; su rol no cambia | ✅ Superado |

## HU-05 · Trazabilidad — `HU05_BitacoraTests`

| ID | Caso | Tipo | Resultado esperado | Resultado |
|---|---|---|---|---|
| CP-32 | Registro → login → logout → login fallido | A | Bitácora: REGISTRO, LOGIN_EXITOSO, LOGOUT, LOGIN_FALLIDO | ✅ Superado |
| CP-33 | Cliente abre /Admin/Usuarios | N | ACCESO_DENEGADO con usuario, ruta y permiso requerido | ✅ Superado |
| CP-34 | Admin cambia un rol | A | CAMBIO_ROL "cliente@inmoarq.do: Cliente → Arquitecto" | ✅ Superado |
| CP-35 | Admin filtra la bitácora por LOGIN_FALLIDO | A | Solo aparecen esos eventos | ✅ Superado |

## Evidencia visual (pruebas manuales)

| Captura | Qué demuestra |
|---|---|
| `evidencia/01_login.png` | Pantalla de inicio de sesión con usuarios de prueba |
| `evidencia/02_registro_validaciones.png` | Validaciones del registro |
| `evidencia/03_registro_exitoso.png` | Cuenta creada |
| `evidencia/04_inicio_cliente_nuevo.png` | Usuario nuevo entra con rol Cliente y menú limitado |
| `evidencia/05_cierre_de_sesion.png` | Cierre de sesión |
| `evidencia/06_cliente_solo_sus_proyectos.png` | Restricción por perfil a nivel de datos |
| `evidencia/07_cliente_acceso_denegado.png` | Acceso no autorizado → 403 |
| `evidencia/08_agente_puede_publicar.png` | Acceso autorizado a una acción |
| `evidencia/09_cuenta_bloqueada.png` | Bloqueo tras 5 intentos fallidos |
| `evidencia/10_inicio_administrador.png` | Tablero completo del administrador |
| `evidencia/11_admin_asigna_rol.png` | Asignación de rol |
| `evidencia/12_matriz_roles_permisos.png` | Matriz de roles y permisos |
| `evidencia/13_bitacora.png` | Trazabilidad |
| `evidencia/14_perfil_permisos.png` | Permisos efectivos del usuario |

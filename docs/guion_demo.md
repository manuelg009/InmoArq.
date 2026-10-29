# Guion para la demostración en vivo (≈ 7 minutos)

Antes de empezar: borra `src/InmoArq.Web/inmoarq.db` para partir de datos limpios, ejecuta `dotnet run --project src/InmoArq.Web` y abre http://localhost:5000. Ten una ventana normal y otra de incógnito.

## 1. Registro de usuarios (1 min)
1. Clic en **Crear cuenta**.
2. Intenta con contraseña `corta` y una confirmación distinta → aparecen las validaciones.
3. Regístrate bien (ej. `estudiante@correo.com` / `Prueba2026`) → "Cuenta creada".
4. Explica: toda cuenta nueva entra como **Cliente** y la contraseña se guarda cifrada.

## 2. Inicio y cierre de sesión (1 min)
1. Entra con la cuenta nueva → el menú solo muestra **Propiedades** y **Diseño arquitectónico**.
2. **Cerrar sesión** → vuelve al login.
3. Opcional: equivoca la contraseña 5 veces con `arquitecto@inmoarq.do` → cuenta bloqueada 15 minutos.

## 3. Asignación de roles y permisos (2 min)
1. Entra como `admin@inmoarq.do` / `Admin2026!`.
2. **Usuarios** → cambia la cuenta nueva a **Agente inmobiliario** → "Rol cambiado".
3. **Roles y permisos** → muestra la matriz y los candados del Administrador.
4. Crea un rol, por ejemplo "Supervisor de obra", y márcale `propiedades.ver`.

## 4. Restricción de funcionalidades según el perfil (2 min)
1. En la ventana de incógnito entra como `cliente@inmoarq.do` / `Cliente2026!`.
   - En **Propiedades** no aparece "Publicar propiedad".
   - En **Diseño arquitectónico** solo ve su proyecto.
2. Escribe a mano la URL `/Admin/Usuarios` → **403 Acceso denegado**.
3. Entra como `agente@inmoarq.do` / `Agente2026!` → sí puede publicar una propiedad.

## 5. Pruebas de acceso autorizado y no autorizado (1 min)
1. Como admin, abre **Bitácora** y filtra por `ACCESO_DENEGADO` → aparece el intento del cliente con la ruta y el permiso que le faltaba.
2. En la terminal ejecuta `dotnet test` → **54 pruebas superadas**, incluida la matriz de 4 roles × 5 módulos.

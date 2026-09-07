# ALCOMAXX Maintenance Tool

Prototipo inicial de una aplicación WPF en español para dirigir trabajos de mantenimiento de Windows 10/11 desde una unidad USB.

## Estado actual

Esta primera entrega implementa la base visual del producto y la preparación de trabajos:

- interfaz original en azul marino, azul, cian y blanco con marca ALCOMAXX dibujada en XAML;
- navegación que presenta el flujo de mantenimiento completo;
- captura de número SAT, cliente y técnico;
- lista de técnicos editable mediante `Config/technicians.txt`;
- inicialización automática de `Config`, `Jobs`, `Reports` y `Tools` junto al ejecutable;
- identificador persistente de la unidad y estado de cada trabajo en JSON;
- plantilla portable de BleachBit 5.0.2, copiada a `Config/bleachbit.ini` sin datos específicos del ordenador original;
- manifiesto que solicita permisos de administrador.
- inventario de aplicaciones registradas para el usuario y el equipo, con clasificación, búsqueda y selección manual;

El inventario guarda la selección dentro del trabajo, pero las operaciones destructivas todavía **no se ejecutan**. Ninguna recomendación se preselecciona y los componentes protegidos no se pueden marcar.

## Estructura esperada en el USB

```text
ALCOMAXX Maintenance Tool/
├── ALCOMAXX Maintenance Tool.exe
├── Config/
│   ├── technicians.txt
│   ├── bleachbit.ini
│   ├── tools.json
│   └── usb-id.txt
├── Jobs/
├── Reports/
└── Tools/
    ├── BleachBit/
    ├── ZHPCleaner/
    └── CCleaner/
```

## Compilar

Requiere el SDK de .NET 8 en Windows:

```powershell
dotnet build .\ALCOMAXX-Maintenance-Tool.sln
dotnet publish .\src\AlcomaxxMaintenance -c Release -r win-x64 --self-contained true
```

### Crear el ZIP para el USB en Windows

Desde PowerShell, en la raíz del repositorio:

```powershell
.\scripts\Build-UsbPackage.ps1
```

El script comprueba que exista .NET 8, publica la aplicación autocontenida, valida que se haya creado `ALCOMAXX Maintenance Tool.exe`, prepara las carpetas del USB y genera:

```text
artifacts\ALCOMAXX-Maintenance-Tool-win-x64.zip
```

Extraiga ese ZIP directamente en el USB. No es necesario instalar .NET en el equipo donde se ejecutará la publicación autocontenida.

### Descargar una compilación desde GitHub

El workflow **Compilar aplicación Windows** también genera el mismo ZIP automáticamente en GitHub Actions. Abra la pestaña **Actions**, seleccione la ejecución más reciente y descargue `ALCOMAXX-Maintenance-Tool-win-x64` en la sección **Artifacts**. También puede iniciarlo manualmente con **Run workflow**. El artefacto se conserva durante 30 días.

La marca del encabezado se dibuja directamente en XAML para que la propuesta de cambios sea completamente textual. El PNG original podrá añadirse posteriormente desde GitHub o durante la preparación final del paquete.

## Perfil de BleachBit

La configuración facilitada se conserva en `src/AlcomaxxMaintenance/Templates/bleachbit.ini`. Al inicializar una unidad, la aplicación la copia a `Config/bleachbit.ini` solamente si el técnico aún no tiene una configuración allí. Se han eliminado la geometría de pantalla, el `hashsalt`, el nombre del usuario y la lista de unidades de trituración del equipo de origen. Las actualizaciones en línea también quedan desactivadas porque BleachBit se ejecutará en modo seguro sin red.

El perfil incluye categorías que eliminan sesiones, cookies, historial, cuarentena de Windows Defender, volcados y otros datos potencialmente útiles. Antes de conectar la ejecución automática, la interfaz mostrará una advertencia y conservará la confirmación de borrado de BleachBit (`delete_confirmation = True`).

## Versiones de herramientas

Las versiones y ejecutables confirmados se guardan en `src/AlcomaxxMaintenance/Templates/tools.json`: BleachBit 5.0.2 utiliza `bleachbit.exe` y `bleachbit_console.exe`, y ZHP Cleaner utiliza `ZHPCleaner.exe`. La versión aprobada de CCleaner es **CCleaner Free 5.44.6575 (64-bit)**; su nombre de ejecutable todavía está pendiente. La aplicación copia este archivo a `Config/tools.json` sin sobrescribir cambios posteriores.

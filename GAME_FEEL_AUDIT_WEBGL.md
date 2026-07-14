# Idle Airport - Game Feel Audit WebGL 16:9

Fecha: 2026-07-02  
Alcance: Unity 6, WebGL, camara fija, pantalla 16:9.  
No validado en Play Mode. La auditoria se basa en escena, prefabs y codigo serializado.

## Resumen ejecutivo

El loop base existe y esta bastante cerca de ser entendible en una pantalla fija: la escena divide el gameplay central en `EntranceSection`, `SecuritySection`, `ShopsSection` y `BoardingLoungeSection`, con HUD superior y paneles laterales para upgrades/ingresos. El jugador puede ver la cadena entrada -> seguridad -> shops -> boarding sin mover camara.

El problema principal de game feel no es el loop sino la falta de feedback localizado. Hoy el feedback visible se concentra en un `HUDFeedbackText` generico y en estados de cards. El click manual tiene pulso de boton, pero no comunica claramente "+AirBucks" ni refuerza al pasajero procesado. El boarding elimina pasajeros instantaneamente y solo muestra "Flight departed", por lo que el cierre del loop puede sentirse invisible.

Hay un bug critico de wiring: en `GameScene.unity`, `PassengerProcessor._aiScanner` esta en `{fileID: 0}` aunque existe `AITScannerStation` con `_isAutoScanner: 1`. Con esa referencia vacia, `HasActiveAIScanner` queda falso y el AI TSA puede comprarse/mostrar estado sin procesar pasajeros automaticamente.

## Evidencia principal

- Layout WebGL fijo: `LeftSidebar`, `CenterGameplayArea`, `RightSidebar` en `Assets/Scenes/GameScene.unity`; el centro contiene `EntranceSection`, `SecuritySection`, `ShopsSection`, `BoardingLoungeSection`.
- Scanner manual: `IdleAirportUIController.OnScannerClicked()` llama a `PassengerProcessor.TryManualScan()` y solo muestra feedback HUD si falla.
- Boton manual: `Assets/Prefabs/P_ScannerButton.prefab` tiene `ImagePulseAnimUI.Play` en `Button.onClick`, con `scaleMultiplier: 1.2`, color amarillo y `cooldown: 0.1`.
- AI scanner: `ScannerStationUIController.AutoProcessRoutine()` posiciona pasajero, espera `_processingDuration` y completa, sin evento de progreso/tick visible.
- Shops: `StoresManager.TotalPassengerIncomeBonus` suma `IncomePerPassenger * OwnedCount`; `PassengerProcessor.GetTotalPassengerReward()` suma ese bonus al reward por pasajero. No es ingreso pasivo por segundo.
- Waiting room: `WaitingRoomUIController.BoardPassengers()` recicla pasajeros inmediatamente y dispara `OnPassengersBoarded(count)`.
- Feedback global: `IdleAirportUIController.ShowHUDFeedback()` muestra un unico texto temporal con cooldown.

## Bugs

### 1. AI TSA Scanner no esta conectado al processor

`Assets/Scenes/GameScene.unity` tiene:

- `PassengerProcessor._aiScanner: {fileID: 0}`
- `AITScannerStation` existe y tiene `_isAutoScanner: 1`

Impacto: aunque se compre el upgrade, `PassengerProcessor.HasActiveAIScanner` queda falso porque depende de `_aiScanner != null && _aiScanner.IsOperational`. Entonces `CanAutoProcessAIScanner` no entra al procesamiento automatico en `Update()`.

Recomendacion: asignar el `ScannerStationUIController` de `AITScannerStation` al campo `_aiScanner` del `PassengerProcessor` en `GameScene`. No requiere cambio de economia.

Archivos/sistemas afectados:

- `Assets/Scenes/GameScene.unity`
- `PassengerProcessor`
- `AITSAScannerUpgrade`

### 2. `PassengerProcessor._debugManualScanState` esta activo en escena

`_debugManualScanState: 1` puede generar logs frecuentes cuando cambia el estado de scan manual. Para WebGL final/prototipo publico, deberia estar apagado salvo sesiones de debug.

Recomendacion: desactivar en escena despues de validar el flujo.

## Mejoras de claridad

### Scanner manual

Estado actual:

- El click llama inmediatamente a `TryManualScan()`.
- El boton tiene pulso visual por prefab.
- En exito no hay texto flotante de AirBucks ni feedback especifico del pasajero.
- En falla se muestra texto HUD: "No passenger ready to board", "Waiting for next flight", etc.
- El pasajero entra a waiting room de forma inmediata (`instantWaitingRoomPlacement: true`), por lo que el scan no tiene momento visible de procesamiento.

Quick wins:

1. Mostrar `+$X` cerca del scanner o del pasajero cuando `TryManualScan()` devuelve true.
2. Hacer un micro-flash/check sobre el pasajero procesado antes de moverlo o al llegar al waiting room.
3. Mantener el pulso del boton, pero diferenciar fallo con shake corto o tint rojo suave, porque hoy el boton pulsa incluso si no se proceso nada.

Implementacion sugerida:

- Agregar evento simple en `PassengerProcessor`: `OnPassengerManuallyProcessed(double reward, PassengerUIVisual passenger)` o, si se quiere evitar exponer visuales, `OnPassengerManuallyProcessed(double reward, Vector2 uiPosition)`.
- Crear `FloatingRewardTextUI` reutilizable con pool chico en Canvas.
- Suscribir una vista de feedback, no `EconomyController`, al evento.

### AI TSA Scanner

Estado actual:

- El scanner automatico usa coroutine con espera fija.
- No hay barra, luz ni pulso durante el procesamiento.
- `CurrentPassengersPerSecond` se muestra como texto, pero el jugador no ve que el scanner "trabaja" salvo que mire pasajeros moverse.
- Cuando se agotan tokens, el HUD muestra mensajes, pero no hay estado visual local del scanner.

Quick wins:

1. Al iniciar auto scan, activar una barra radial/lineal simple o fill sobre el scanner.
2. Al completar scan, emitir pulso/luz en `AITScannerStation` y `+$X` localizado.
3. Al subir PPS, hacer un pulso breve en el scanner automatico y en el texto de PPS.
4. Usar color/acento distinto para manual y AI: manual = boton/scan amarillo; AI = azul/cian operativo, gris sin tokens.

Implementacion sugerida:

- En `ScannerStationUIController`, exponer eventos locales:
  - `OnAutoProcessingStarted(float duration)`
  - `OnAutoProcessingProgress(float normalized)` si se necesita barra sin acoplar al UI controller.
  - `OnAutoProcessingCompleted(PassengerUIVisual passenger)`
- Alternativa de menor costo: una `AutoScannerFeedbackView` lee `IsBusy` y `ProcessingDuration` solo para animar, pero es menos precisa que eventos.

### Shops

Estado actual:

- Las shops se compran desde cards.
- `ShopVisualDisplayController` crea/actualiza visuales en la zona de shops cuando `OnStorePurchased` dispara.
- La economia actual es bonus por pasajero, no ingreso pasivo por tiempo.
- Las cards dicen `+$X/pax`, pero el jugador puede interpretar "shop" como ingreso autonomo si no ve el bonus aparecer en cada pasajero.

Quick wins:

1. Al comprar shop, hacer spawn/pulse en la zona de shops, no solo `HUDFeedbackText`.
2. En cada pasajero procesado, si hay bonus de shops, mostrar reward dividido o con etiqueta: `+$1 base +$5 shops`.
3. En `ShopVisualItemView`, pulso corto al subir `OwnedCount`.
4. En cards, mostrar `Owned: N` o `Lv. N` tambien en la card de compra, no solo en el visual construido.

Implementacion sugerida:

- Agregar evento `OnShopPurchased(int index, Store store)` ya existe; usarlo en una vista de feedback local.
- Para income generado, no crear ingreso pasivo nuevo. Emitir desde `PassengerProcessor.CompleteProcessedPassenger()` un evento con desglose:
  - base income
  - shop bonus
  - total reward

### Waiting room y boarding

Estado actual:

- Waiting room tiene capacidad y reservas.
- `IdleAirportUIController` muestra `Gate: current/capacity` y agrega `| Boarding` cuando esta lleno.
- Hay countdown `Next flight: 00s` si hay pasajeros.
- Boarding recicla pasajeros instantaneamente y solo muestra "Flight departed".

Quick wins:

1. Cambiar mensaje a `Flight departed: -8 pax` o `8 passengers boarded` para que el evento tenga escala.
2. Al embarcar, animar los pasajeros hacia el borde/puerta durante 0.2-0.4s antes de reciclarlos.
3. Pulse en `BoardingLoungeSection` cuando sale un batch.
4. Cuando waiting room esta llena, mostrar un estado local cerca del gate, no solo bloquear el scan con mensaje global.

Implementacion sugerida:

- Mantener `OnPassengersBoarded(int count)`.
- Agregar `OnWaitingRoomFull` solo cuando cruza de no lleno a lleno, para evitar spam.
- Mover la animacion de salida a una vista/animador de waiting room; no meterla en economia.

### HUD/cards WebGL

Estado actual:

- HUD superior muestra dinero, PPS y pasajeros.
- Panel derecho muestra ingresos base/shops/total.
- Panel izquierdo muestra upgrades/stores.
- Botones/cards tienen estados visuales `Available`, `NeedMoney`, `Locked`.
- El feedback global top-center puede competir con el HUD y no indica donde ocurrio el evento.

Quick wins:

1. Mantener AirBucks como primer dato dominante.
2. Hacer pulso del dinero cuando sube y tint rojo corto cuando una compra falla.
3. En cards no comprables por dinero, cambiar action text a `Need $X` o mantener precio pero con estado visual mas obvio.
4. Mostrar el proximo objetivo en una sola linea contextual: "Next: AI Scanner $25" o "Next: Coffee Store $25", calculado desde cards disponibles.
5. Evitar repetir feedback en HUD si el evento ya tiene feedback localizado; reservar HUD para estados globales: sin capacidad, sin tokens, compra fallida.

## Mejoras de satisfaccion

### Microinteracciones faltantes

Agregar feedback localizado para:

- Click manual exitoso: pulso + texto flotante.
- Click sin efecto: shake/tint local + mensaje corto.
- Compra exitosa: card pulse + visual nuevo en terminal.
- Falta de dinero: action area tint rojo/naranja + HUD breve.
- Shop comprado: spawn/pulse en `ShopsSection`.
- Income generado: `+$total` cerca del scanner/waiting room; opcional desglose si hay shop bonus.
- Pasajeros embarcados: salida visual por batch + contador `-N boarded`.
- Waiting room llena: borde/pulse del gate.
- Upgrade adquirido: pulse en scanner automatico o card.

### Estrategia reusable

No hace falta un event bus. Usar eventos C# en los sistemas existentes y componentes de feedback suscriptos:

- `PassengerProcessor`
  - `OnPassengerManuallyProcessed`
  - `OnPassengerAutoProcessed`
  - `OnPassengerProcessFailed`
- `EconomyController`
  - Ya tiene `OnMoneyChanged`; si se necesita feedback de delta, agregar `OnMoneyDelta(double amount, EconomyChangeReason reason)`.
- `StoresManager`
  - Ya tiene `OnStorePurchased` y `OnStorePurchaseFailed`.
- `WaitingRoomUIController`
  - Ya tiene `OnOccupancyChanged` y `OnPassengersBoarded`.
  - Agregar `OnWaitingRoomFull` por cruce de estado.
- `AITSAScannerUpgrade`
  - Ya tiene eventos de tokens, durabilidad y compra; falta usar esos eventos para feedback visual local del scanner.

Componentes propuestos:

- `FloatingRewardTextUI`: pool de textos flotantes.
- `ScannerFeedbackView`: escucha manual/auto scan y anima scanner.
- `PurchaseCardFeedbackView`: pulso/shake generico para cards.
- `WaitingRoomFeedbackView`: full pulse, batch boarding animation.
- `ShopFeedbackView`: pulse/spawn al comprar y al aportar bonus.

## Mejoras de riesgo medio

1. Separar `IdleAirportUIController` en coordinador de textos HUD y feedback visual localizado. Hoy maneja botones, textos, feedback, stores, AI, waiting room y countdown.
2. Cambiar `ScannerStationUIController.AutoProcessRoutine()` para reportar progreso visual. Es simple, pero toca flujo automatico.
3. Boarding animado antes de recycle. Puede afectar pooling si se hace dentro de `WaitingRoomUIController`; mejor implementarlo con una coroutine controlada y cuidado de no duplicar pasajeros.
4. Reward breakdown por pasajero. Requiere cambiar firma interna de reward para saber base/shops/total, pero no cambia numeros.

## Cambios que no conviene hacer ahora

- No rehacer todo el HUD.
- No cambiar el core loop.
- No convertir shops en ingreso pasivo por segundo; el codigo actual las define como bonus por pasajero.
- No agregar dependencias nuevas de tweening si no existe una ya integrada. Coroutines simples con `RectTransform`, `Image`, `CanvasGroup` alcanzan.
- No crear un event bus global.
- No tocar balance numerico salvo que se confirme bug.
- No editar escenas/prefabs sin documentar exactamente los campos a asignar.

## Orden recomendado de implementacion

1. Corregir wiring de `_aiScanner` en `GameScene`.
2. Apagar `_debugManualScanState` para build WebGL.
3. Agregar eventos de procesamiento manual/auto en `PassengerProcessor`.
4. Implementar `FloatingRewardTextUI` para `+$AirBucks`.
5. Agregar `ScannerFeedbackView` para manual/AI: pulso exitoso, fallo, auto tick/progreso.
6. Usar `OnStorePurchased` para pulse/spawn en `ShopsSection`.
7. Agregar desglose visual de bonus shops cuando `shopBonus > 0`.
8. Mejorar boarding: mensaje con cantidad + pulso/salida visual de batch.
9. Ajustar cards: estado `Need money`, owned count visible, pulse de compra.
10. Revisar manualmente en Game View 16:9.

## Campos de escena/prefab a revisar manualmente

- `PassengerProcessor._aiScanner`: asignar `AITScannerStation` / `ScannerStationUIController`.
- `PassengerProcessor._debugManualScanState`: desactivar para experiencia final.
- `HUDFeedbackText`: confirmar que no tapa informacion principal del HUD en 16:9.
- `ShopVisualDisplayController._builtContainer` y `_itemTemplate`: ya estan serializados; revisar visualmente que el spawn no se superponga.
- `WaitingRoomUIController`: confirmar que capacidad visual y batch se entienden en una sola pantalla.
- `P_ScannerButton`: confirmar que el pulso actual se percibe; si no, subir contraste/duracion levemente.

## Checklist manual sugerido

- Probar en Game View 16:9.
- Comprar AI TSA y confirmar que procesa automaticamente sin mirar solo el texto PPS.
- Click manual exitoso: verificar respuesta inmediata, dinero y pasajero.
- Click bloqueado: verificar que el fallo se entiende sin leer logs.
- Comprar cada shop: verificar cambio visible en `ShopsSection`.
- Procesar pasajeros con shops comprados: confirmar que el bonus por pasajero se entiende.
- Llenar waiting room: verificar estado lleno y bloqueo del scanner.
- Esperar boarding por batch: verificar que se siente como recompensa/cierre de loop.

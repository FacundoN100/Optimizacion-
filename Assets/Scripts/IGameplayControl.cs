// Puerto (interfaz) con lo que otros sistemas de la escena pueden pedirle al juego.
// Por ahora lo usa el tutorial; cuando el juego pase a waves, acá se agregan
// o se cambian los métodos sin que los que lo usan conozcan al GameManager.
public interface IGameplayControl
{
    void SetCarMovement(bool enabled);
    void StartClock();
}

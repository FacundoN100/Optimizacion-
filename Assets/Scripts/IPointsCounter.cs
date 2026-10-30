// Puerto (interfaz): lo único que un arco de puntos necesita saber
// es que hay "algo" que suma puntos. No le importa que sea la FinishLine.
public interface IPointsCounter
{
    void IncreasePoints();
}

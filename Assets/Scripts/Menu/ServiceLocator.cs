using System;
using System.Collections.Generic;

// Service Locator (Unidad 01, nivel 3 de resolución de dependencias).
// Es la "guía" central de servicios: el Bootstrap anota cada servicio con su
// interfaz y cualquier sistema que lo necesite lo pide por esa interfaz,
// sin conocer la clase concreta que lo implementa.
//
// Es el mismo código que mostró el profe en los ejemplos de la Unidad 01
// (clase Services) y de la Unidad 05 (clase ServiceLocator).
public static class ServiceLocator
{
    // Clave: el tipo con el que se registró el servicio (por ejemplo, IAudioSettings).
    // Valor: el objeto que cumple ese contrato.
    private static readonly Dictionary<Type, object> _services = new();

    // Anota (o reemplaza) el servicio para el tipo T.
    public static void Register<T>(T service) => _services[typeof(T)] = service;

    // Devuelve el servicio registrado para el tipo T.
    // Si nadie lo registró antes, da error en tiempo de ejecución:
    // por eso el Bootstrap tiene que registrar todo antes que nadie lo pida.
    public static T Get<T>() => (T)_services[typeof(T)];

    // Borra el servicio registrado para T, pero SOLO si sigue siendo este mismo objeto.
    // Lo que se registra, se desregistra: misma idea que el Register/Unregister del
    // Custom Update Manager (Unidad 04) y que desuscribir eventos para evitar
    // memory leaks (Unidad 03). Sin esto, el diccionario estático seguiría
    // apuntando a objetos de una escena que ya se destruyó.
    public static void Unregister<T>(T service)
    {
        // TryGetValue: lo mismo que usa el profe en AssetsService (Unidad 05).
        // ReferenceEquals: pregunta si son exactamente el mismo objeto.
        if (_services.TryGetValue(typeof(T), out object registered) && ReferenceEquals(registered, service))
        {
            _services.Remove(typeof(T));
        }
    }
}
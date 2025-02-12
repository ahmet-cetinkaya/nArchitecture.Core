namespace NArchitecture.Core.Persistence.Abstractions.Repositories;

public interface IEntity<T>
{
    T Id { get; set; }
}

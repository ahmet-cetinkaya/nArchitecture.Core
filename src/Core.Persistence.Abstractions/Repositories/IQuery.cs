namespace NArchitecture.Core.Persistence.Abstractions.Repositories;

public interface IQuery<T>
{
    IQueryable<T> Query();
}

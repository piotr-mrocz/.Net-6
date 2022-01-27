using MinimalnieAPI;

namespace ToDos.MinimalAPI;

public interface IToDoService
{
    void Create(ToDo toDo);

    void Delete(Guid id);

    List<ToDo> GetAll();

    ToDo GetById(Guid id);

    void Update(ToDo toDo);
}
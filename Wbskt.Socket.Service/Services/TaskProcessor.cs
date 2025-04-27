namespace Wbskt.Socket.Service.Services;

public class TaskProcessor
{
    private readonly Queue<Task> taskQueue = new Queue<Task>();

    private bool running;

    private static TaskProcessor? _instance;

    private TaskProcessor()
    {
        _instance = this;
    }

    public static TaskProcessor GetInstance()
    {
        return _instance ?? new TaskProcessor();
    }

    public static void Enqueue(Task task)
    {
        GetInstance().taskQueue.Enqueue(task);
    }

    // this must be only called once. preferably from the Main()
    public void Run(ILogger<TaskProcessor> logger, CancellationToken ct)
    {
        if (running)
        {
            return;
        }

        running = true;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var count = 100;
                var tasks = new List<Task>();
                while (taskQueue.Count > 0 && count > 0)
                {
                    count--;
                    tasks.Add(taskQueue.Dequeue());
                }

                Task.WhenAll(tasks).Wait(ct);
            }
            catch (Exception ex)
            {
                logger.LogError("error while processing tasks: {message}", ex.Message);
            }
        }
    }
}

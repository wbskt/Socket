namespace Wbskt.Server.Services
{
    public class TaskExcecuter
    {
        private readonly Queue<Task> taskQueue = new Queue<Task>();

        private bool running = false;

        private static TaskExcecuter? instance;

        private TaskExcecuter()
        {
            instance = this;
        }

        public static TaskExcecuter GetInstance()
        {
            if (instance == null)
            {
                return new TaskExcecuter();
            }

            return instance;
        }

        public static void Enqueue(Task task)
        {
            GetInstance().taskQueue.Enqueue(task);
        }

        // this must be only called once. preferably from the Main()
        public void Run(CancellationToken ct) 
        {
            if (running) return;
            
            running = true;
            while (true && !ct.IsCancellationRequested)
            {
                var count = 100;
                var tasks = new List<Task>();
                while (taskQueue.Count > 0 && count > 0)
                {
                    count--;
                    tasks.Add(taskQueue.Dequeue());
                }

                Task.WhenAll(tasks).Wait();
            }
        }
    }
}

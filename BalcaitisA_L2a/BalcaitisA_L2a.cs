using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace BalcaitisA_L2a
{   
    class BalcaitisA_L2a
    {
        public const int ArraySize = 10;
        public const int ProductsSize = 100;
        /*
         * Class contains producer type products
         */
        class ProducerProducts
        {
            public string product { set; get; }
            public int count { set; get; }
            public double price { set; get; }
            public ProducerProducts()
            {
                product = "";
                count = 0;
                price = 0.0;
            }
        }
        /*
         * Producer class
         */
        class Producer
        {
            public string name { set; get; }
            public int size { set; get; }
            public ProducerProducts[] ProducerProducts = new ProducerProducts[ArraySize];
            public Producer()
            {
                name = "";
                size = 0;
            }
            /*
            * Method to run when producer thread starts
            * @param B common array object
            */
            public void ThreadRun(Buffer B)
            {
                for (int i = 0; i < this.size; i++)
                {
                    for (int j = 0; j < ProducerProducts[i].count; j++)
                    {
                        B.WriteToArray(ProducerProducts[i].product);  // "producing"
                    }
                }
            }
        }
        /*
         * Class contains consumer type products
         */
        class ConsumerProducts
        {
            public string product { set; get; }
            public int count { set; get; }
            public ConsumerProducts()
            {
                product = "";
                count = 0;
            }
            public void Consume()
            {
                count--;
            }
        }
        /*
         * Consumer class
         */
        class Consumer
        {
            public string name { set; get; }
            public int size { set; get; }
            public ConsumerProducts[] ConsumerProducts = new ConsumerProducts[ArraySize];
            public Consumer()
            {
                for (int i = 0; i < ArraySize; i++)
                    ConsumerProducts[i] = new ConsumerProducts();
                name = "";
                size = 0;
            }
            /*
            * Method to run when consumer thread starts
            * @param B common array object
            */
            public void ThreadRun(Buffer B)
            {
                while (!B.allProducersFinished) { }
                for (int i = 0; i < this.size; i++)
                {
                    for (int j = 0; j < ConsumerProducts[i].count; j++)
                    {
                        B.ReadFromArray(ConsumerProducts[i].product);// "consuming
                    }
                }
            }
        }
        /*
         * Common array class with critical sections initalizations
         */
        class Buffer
        {
            private ConsumerProducts[] B = new ConsumerProducts[ProductsSize];
            public int size { set; get; }
            private Object m_lock = new Object();
            public bool allProducersFinished { set; get; }
            public Buffer()
            {
                for (int i = 0; i < ProductsSize; i++)
                {
                    B[i] = new ConsumerProducts();
                }
                size = 0;
                allProducersFinished = false;
            }
            /**
              * Checks if product exists in common array
              * @param name product name
              */
            public int ifExists(string name)
            {
                int index = -1;
                for (int i = 0; i < size; i++)
                {
                    if (name.CompareTo(B[i].product) == 0) //if finds in array
                    {
                        index = i;
                        break;
                    }
                }
                return index;
            }
            /**
              * Returns product name at index location
              * @param index product location in common array
              */
            public string getName(int index) { return B[index].product; }
            bool readerFlag = false;  // State flag
            /**
              * Consumes product from common array entering critical section
              * @param name product name
              */
            public void ReadFromArray(string name)
            {
                
                int index = -1; // from what position to consume
                lock (m_lock)   // Enter synchronization block
                {
                    
                    while ((index = ifExists(name)) == -1 && !readerFlag)
                    {            // Wait until Buffer.WriteToArray is done producing
                        try
                        {
                            // Waits for the Monitor.Pulse in WriteToCell
                            Monitor.Wait(m_lock);
                            
                        }
                        catch (SynchronizationLockException e)
                        {
                            Console.WriteLine(e);
                        }
                        catch (ThreadInterruptedException e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                    if (index != -1)
                    {

                        if (B[index].count > 1)
                            B[index].count--;
                        else
                            remove(index);
                    }
                    readerFlag = true; ;    // Reset the state flag to say consuming
                    // is done.
                    Monitor.Pulse(m_lock);   // Pulse tells Cell.WriteToCell that
                    // Cell.ReadFromCell is done.
                }   // Exit synchronization block
            }
            /**
              * Produces product to common array entering critical section
              * @param name product name
              */
            public void WriteToArray(string name)
            {
                lock (m_lock)  // Enter synchronization block
                {
                    int ind = ifExists(name);
                    if (ind == -1)
                    {
                        ind = RequestInsertIndex(name);
                        insert(name, ind);
                    }
                    else
                    {
                        B[ind].count++;
                    }
                    readerFlag = true;    // Reset the state flag to say producing
                    // is done
                    Monitor.Pulse(m_lock);  // Pulse tells Cell.ReadFromCell that 
                    // Cell.WriteToCell is done.
                }   // Exit synchronization block
            }

            /**
              * Removes product from common array at index location
              * @param index product index in common array
              */
            public void remove(int index)
            {
                if (index == size - 1)
                {
                    B[index].product = "";
                    B[index].count = 0;
                }
                else
                {
                    for (int i = index; i < size; i++)
                    {
                        B[i] = B[i + 1];
                    }
                    B[size - 1].product = "";
                    B[size - 1].count = 0;
                }
                --size;
            }
            /**
              * Inserts product to index location in common array
              * @param name product name
              * @param index index to where insert product
              */
            public void insert(string name, int index)
            {
                ConsumerProducts temp = new ConsumerProducts();
                temp.product = name;
                temp.count = 1;
                size++;
                if (size == 1)
                {
                    B[index] = temp;
                }
                else
                {
                    for (int i = size; i > index + 1; i--)
                    {
                        B[i - 1] = B[i - 2];
                    }
                    B[index] = temp;
                }
            }

            /**
              * Gets product index from common array
              * @param name product name
              */
            public int RequestInsertIndex(string name)
            {
                int index = -1;
                if (size == 0) return 0;
                if (size == 1)
                    if (name.CompareTo(B[0].product) > 0)
                        return 1;
                    else return 0;
                if (name.CompareTo(B[size - 1].product) > 0)
                    return size;
                for (int i = 0; i < size; i++)
                {
                    if (name.CompareTo(B[i].product) < 0)
                    {
                        index = i;
                        return index;
                    }
                }
                return index;
            }
            /**
              * Writes data to results file
              * @param resultsFile results file name
              */
            public void writeData(string resultsFile)
            {
                StreamWriter file = new StreamWriter(resultsFile, true);
                file.WriteLine();
                file.WriteLine();
                file.WriteLine("Common Array size: " + size + "\n");
                if(size != 0) file.WriteLine("{0,10}{1,10}", "Product", "Count");
                file.WriteLine("---------------------------------");
                for (int i = 0; i < size; i++)
                {
                    file.Write(i + 1 + ") ");
                    file.WriteLine("{0,10}{1,10}", B[i].product, B[i].count);
                }
                file.WriteLine();
                file.Close();
            }
        }


        static void Main(string[] args)
        {
            //string dataFile = "BalcaitisA_L2a_dat_1.txt";
            //string dataFile = "BalcaitisA_L2a_dat_2.txt";
            string dataFile = "BalcaitisA_L2a_dat_3.txt";
            string resultsFile = "BalcaitisA_L2a_res.txt";
            int ConsumersSize = 0;
            int ProducersSize = 0;
            Producer[] Producers = new Producer[ArraySize];
            Consumer[] Consumers = new Consumer[ArraySize];
            Buffer B = new Buffer();

            read(dataFile, Producers, Consumers, out ProducersSize, out ConsumersSize);
            writeTables(resultsFile, Producers, Consumers, ProducersSize, ConsumersSize);

            Thread producer1 = new Thread(() => Producers[0].ThreadRun(B));
            Thread producer2 = new Thread(() => Producers[1].ThreadRun(B));
            Thread producer3 = new Thread(() => Producers[2].ThreadRun(B));
            Thread producer4 = new Thread(() => Producers[3].ThreadRun(B));
            Thread producer5 = new Thread(() => Producers[4].ThreadRun(B));

            Thread consumer1 = new Thread(() => Consumers[0].ThreadRun(B));
            Thread consumer2 = new Thread(() => Consumers[1].ThreadRun(B));
            Thread consumer3 = new Thread(() => Consumers[2].ThreadRun(B));
            Thread consumer4 = new Thread(() => Consumers[3].ThreadRun(B));
            Thread consumer5 = new Thread(() => Consumers[4].ThreadRun(B));
            
            producer1.Start();
            producer2.Start();
            producer3.Start();
            producer4.Start();
            producer5.Start();

            consumer1.Start();
            consumer2.Start();
            consumer3.Start();
            consumer4.Start();
            consumer5.Start();

            producer1.Join();
            producer2.Join();
            producer3.Join();
            producer4.Join();
            producer5.Join();
            B.allProducersFinished = true;
            consumer1.Join();
            consumer2.Join();
            consumer3.Join();
            consumer4.Join();
            consumer5.Join();

            B.writeData(resultsFile);
            Console.WriteLine("Darbas baigtas!");
            Console.Read();
        }
        /**
        * Writes data to results file
        * @param resultsFile presults file name
        * @param Producers producers data array
        * @param Consumers consumers data array
        * @param ProducersSize producers array size
        * @param ConsumersSize consumers array size
        */
        static void writeTables(string resultsFile, Producer[] Producers, Consumer[] Consumers, int ProducersSize, int ConsumersSize)
        {
            StreamWriter file = new StreamWriter(resultsFile);
            file.WriteLine("Producers size: " + ProducersSize + "\n");
            
            for (int i = 0; i < ProducersSize; i++)
            {
                file.WriteLine(Producers[i].name);
                file.WriteLine("{0,10}{1,10}{2,10}", "Product", "Count", "Price");
                file.WriteLine("---------------------------------");
                for (int j = 0; j < Producers[i].size; j++)
                {
                    file.Write(j+1 + ") ");
                    file.WriteLine("{0,10}{1,10}{2,10}", Producers[i].ProducerProducts[j].product, Producers[i].ProducerProducts[j].count, Producers[i].ProducerProducts[j].price);
                }
                file.WriteLine();
            }
            file.WriteLine();
            file.WriteLine("---------------------------------");
            file.WriteLine();
            file.WriteLine("Consumers size: " + ProducersSize + "\n");

            for (int i = 0; i < ConsumersSize; i++)
            {
                file.WriteLine(Consumers[i].name);
                file.WriteLine("{0,10}{1,10}", "Product", "Count");
                file.WriteLine("---------------------");
                for (int j = 0; j < Consumers[i].size; j++)
                {
                    file.Write(j + 1 + ") ");
                    file.WriteLine("{0,10}{1,10}", Consumers[i].ConsumerProducts[j].product, Consumers[i].ConsumerProducts[j].count);
                }
                file.WriteLine();
            }
            file.WriteLine();
            file.Close();
        }
        /**
        * Reads data from data file
        * @param dataFile data file name
        * @param Producers producers data array
        * @param Consumers consumers data array
        * @param ProducersSize producers array size
        * @param ConsumersSize consumers array size
        */
        static void read(string dataFile, Producer[] Producers, Consumer[] Consumers, out int ProducersSize, out int ConsumersSize)
        {
            StreamReader file = new StreamReader(dataFile);
            ConsumersSize = 0;
            ProducersSize = 0;
            string line = file.ReadLine();
            ProducersSize = Int32.Parse(line);
            for (int i = 0; i < ProducersSize; i++)
            {
                Producer P = new Producer();
                line = file.ReadLine();
                string[] LineElement = line.Split(new string[] { "\n", "\r\n", "\t" }, StringSplitOptions.RemoveEmptyEntries);
                P.name = LineElement[0];
                P.size = Int32.Parse(LineElement[1]);
                for (int j = 0; j < P.size; j++)
                {
                    line = file.ReadLine();
                    LineElement = line.Split(new string[] { "\n", "\r\n", "\t" }, StringSplitOptions.RemoveEmptyEntries);
                    ProducerProducts temp = new ProducerProducts();
                    temp.product = LineElement[0];
                    temp.count = Int32.Parse(LineElement[1]);
                    temp.price = Double.Parse(LineElement[2]);
                    P.ProducerProducts[j] = temp;
                    Producers[i] = P;
                }
            }
            line = file.ReadLine();
            ConsumersSize = Int32.Parse(line);
            for (int i = 0; i < ConsumersSize; i++)
            {
                Consumer C = new Consumer();
                line = file.ReadLine();
                string[] LineElement = line.Split(new string[] { "\n", "\r\n", "\t" }, StringSplitOptions.RemoveEmptyEntries);
                C.name = LineElement[0];
                C.size = Int32.Parse(LineElement[1]);
                for (int j = 0; j < C.size; j++)
                {
                    line = file.ReadLine();
                    LineElement = line.Split(new string[] { "\n", "\r\n", "\t" }, StringSplitOptions.RemoveEmptyEntries);
                    ConsumerProducts temp = new ConsumerProducts();
                    temp.product = LineElement[0];
                    temp.count = Int32.Parse(LineElement[1]);
                    C.ConsumerProducts[j] = temp;
                    Consumers[i] = C;
                }
            }
        }
    }
}

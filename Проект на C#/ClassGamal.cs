using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TI_3
{
    class ClassGamal
    {
        public int P { get; private set; } 
        public int G { get; private set; }  
        public int X { get; private set; }  
        public int Y { get; private set; } 
        public List<int> GList { get; private set; }

        private const int MillerRabinIterations = 10;
        public ClassGamal(int p, int x)
        {
            if (!isPrime(p)) throw new ArgumentException("число P должно быть простым");

            if (x <= 1 || x >= p - 1) throw new ArgumentException("закрытый ключ X должен быть в диапазоне (1, P-1)");
            
            P = p;
            X = x;
            GList = FindPrimitiveRoots(p);
        }

        public bool SetG(int g)
        {
            if (!GList.Contains(g))
            {
                return false;
            }
            G = g;
            return true;
        }

        public void SetY()
        {
            Y = ModPow(G, X, P);
        }

        public (int, int) EncryptByte(byte m, int k)
        {
            int a = ModPow(G, k, P); // G^k mod P
            int b = (ModPow(Y, k, P) * m) % P; // (Y^k * m) mod P
            return (a, b);
        }

        public byte DecryptByte(int a, int b)
        {
            int s = ModPow(a, X, P); // a^X mod P
            int sInv = ModInverse(s, P);
            int m = (b * sInv) % P; // b * s^(-1) mod P
            return (byte)m;
        }

        public MemoryStream EncryptFile(MemoryStream input, int k)
        {
            var output = new MemoryStream();

            for (int OutByte = input.ReadByte(); OutByte != -1; OutByte = input.ReadByte())
            {
                if (OutByte >= P)
                {
                    throw new Exception("значение байта больше либо равно P");
                }
                var (a, b) = EncryptByte((byte)OutByte, k);
                byte[] intBytes = BitConverter.GetBytes(a);
                output.Write(intBytes, 0, intBytes.Length);

                intBytes = BitConverter.GetBytes(b);
                output.Write(intBytes, 0, intBytes.Length);
            }
            output.Position = 0;
            return output;
        }

        public MemoryStream DecryptFile(MemoryStream input)
        {
            var output = new MemoryStream();
            byte[] buffer = new byte[4]; 
            for (int i = input.Read(buffer, 0, 4); i != 0; i = input.Read(buffer, 0, 4))
            {
                int a = BitConverter.ToInt32(buffer, 0);
                input.Read(buffer, 0, 4);
                int b = BitConverter.ToInt32(buffer, 0);
                byte m = DecryptByte(a, b);
                output.WriteByte(m);
            }
            output.Position = 0;
            return output;

        }

        //быстрое модульное возведение в степень
        public static int ModPow(int a, int exponent, int modulus)
        {
            int result = 1;

            while (exponent > 0)
            {
                while (exponent % 2 == 0) // => возвести в квадрат a и обновить показатель степени
                {
                    exponent /= 2;
                    a = (a * a) % modulus;
                }

                result = (result * a) % modulus; // => умножить результат на a и уменьшите показатель степени
                exponent--;
            }

            return result;
        }

        // Мод мультипликативн обр - расширенный алгоритм Евклида
        public static int ModInverse(int a, int mod)
        {
            // 1 инициализация
            int m0 = mod, t, q;
            int x0 = 0, x1 = 1;

            if (mod == 1) return 0;
            // 2 отслеж коэф
            while (a > 1)
            {
                q = a / mod;
                t = mod;

                mod = a % mod; a = t;
                t = x0;

                x0 = x1 - q * x0;
                x1 = t;
            }
            // 3 модульное обратное число, убедившись, что оно положительное
            return x1 < 0 ? x1 + m0 : x1;
        }

        //Нахождение первообразных корней
        public static List<int> FindPrimitiveRoots(int p)
        {
            List<int> primitiveRoots = new();
            // 1 функцию Эйлера
            int phi = p - 1;
            // 2 разложение на простые множители
            var factors = PrimeFactors(p - 1);

            for (int g = 2; g < p; g++)
            {
                bool flag = true;
                foreach (var q in factors)
                {
                    if (ModPow(g, phi / q, p) == 1)
                    {
                        flag = false;
                        break;
                    }
                }
                // доп примитивн корень
                if (flag) primitiveRoots.Add(g);
            }
            return primitiveRoots;
        }

        public static HashSet<int> PrimeFactors(int n)
        {
            HashSet<int> factors = new();
            for (int i = 2; i * i <= n; i++)
            {
                while (n % i == 0)
                {
                    factors.Add(i);
                    n /= i;
                }
            }
            if (n > 1) factors.Add(n);
            return factors;
        }

        public static bool isPrime(int val)
        {
            if (val <= 1) return false;
            if (val == 2) return true;
            if (val % 2 == 0) return false;

            int boundary = (int)Math.Floor(Math.Sqrt(val));

            for (int i = 3; i <= boundary; i += 2)
            {
                if (val % i == 0)
                    return false;
            }

            //// Тест Миллера-Рабина 
            //int r = 0;
            //int d = val - 1;
            //while (d % 2 == 0)
            //{
            //    d /= 2;
            //    r++;
            //}

            //int[] testValues = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29 };
            //foreach (int a in testValues)
            //{
            //    if (a >= val) continue;

            //    if (!MillerRabinTest(a, d, val, r))
            //        return false;
            //}

            return true;
        }
        private static bool MillerRabinTest(int a, int d, int n, int r)
        {
            int x = ModPow(a, d, n);
            if (x == 1 || x == n - 1)
                return true;

            for (int i = 0; i < r - 1; i++)
            {
                x = ModPow(x, 2, n);
                if (x == n - 1)
                    return true;
            }

            return false;
        }
        
        private static bool MillerRabinTest(int n, int k)
        {
            //  n - 1 в виде 2^r * d, где d нечетное
            int r = 0;
            int d = n - 1;
            while (d % 2 == 0)
            {
                d /= 2;
                r++;
            }

            Random rnd = new Random();
            for (int i = 0; i < k; i++)
            {
                //  a в диапазоне [2, n-2]
                int a = rnd.Next(2, n - 1);

                //  x = a^d mod n
                int x = ModPow(a, d, n);

                if (x == 1 || x == n - 1)
                    continue;

                // повторяем r-1 раз
                bool isProbablePrime = false;
                for (int j = 0; j < r - 1; j++)
                {
                    x = ModPow(x, 2, n);
                    if (x == n - 1)
                    {
                        isProbablePrime = true;
                        break;
                    }
                }

                if (!isProbablePrime)
                    return false;
            }

            return true;
        }
        public static bool isRelativelyPrime(int n, int m)
        {
            return ExtendedEuclidean(n, m, out int x, out int y) == 1;
        }
        private static int ExtendedEuclidean(int a, int b,out int x, out int y)
        {
            int dividend = a, divisor = b;
            int x_prev = 1, x_curr = 0;
            int y_prev = 0, y_curr = 1;
            int quotient, remainder, x_next, y_next;

            while (divisor != 0)
            {
                quotient = dividend / divisor;
                remainder = dividend % divisor;
                dividend = divisor;
                divisor = remainder;

                x_next = x_prev - quotient * x_curr;
                x_prev = x_curr;
                x_curr = x_next;

                y_next = y_prev - quotient * y_curr;
                y_prev = y_curr;
                y_curr = y_next;
            }

            x = x_prev;
            y = y_prev;
            return dividend; 
        }
    }
}

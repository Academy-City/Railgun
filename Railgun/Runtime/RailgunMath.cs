using System;

namespace Railgun.Runtime
{
    public static class RailgunMath
    {
        public static object Add(object x, object y)
        {
            switch (x) {
                case int a:
                    switch (y) {
                        case int b: return a + b;
                        case double b: return a + b;
                    }
                    break;
                case double a:
                    switch (y) {
                        case int b: return a + b;
                        case double b: return a + b;
                    }
                    break;
            }
            throw new ArgumentException($"{x}, {y}");
        }
        
        public static object Sub(object x, object y) {
            switch (x) {
                case int a:
                    switch (y) {
                        case int b: return a - b;
                        case double b: return a - b;
                    }
                    break;
                case double a:
                    switch (y) {
                        case int b: return a - b;
                        case double b: return a - b;
                    }
                    break;
            }
            throw new ArgumentException($"{x}, {y}");
        }
        
        public static object Mul(object x, object y) {
            switch (x) {
                case int a:
                    switch (y) {
                        case int b: return a * b;
                        case double b: return a * b;
                    }
                    break;
                case double a:
                    switch (y) {
                        case int b: return a * b;
                        case double b: return a * b;
                    }
                    break;
            }
            throw new ArgumentException($"{x}, {y}");
        }
        
        public static object Div(object x, object y) {
            switch (x) {
                case int a:
                    switch (y) {
                        case int b: return a / b;
                        case double b: return a / b;
                    }
                    break;
                case double a:
                    switch (y) {
                        case int b: return a / b;
                        case double b: return a / b;
                    }
                    break;
            }
            throw new ArgumentException($"{x}, {y}");
        }
        
        public static object Le(object x, object y) {
            switch (x) {
                case int a:
                    switch (y) {
                        case int b: return a <= b;
                        case double b: return a <= b;
                    }
                    break;
                case double a:
                    switch (y) {
                        case int b: return a <= b;
                        case double b: return a <= b;
                    }
                    break;
            }
            throw new ArgumentException($"{x}, {y}");
        }
    }
}
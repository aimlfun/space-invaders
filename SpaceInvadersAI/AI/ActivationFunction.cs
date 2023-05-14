using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceInvadersAI.AI;

//
//    █      ███    █████    ███    █   █     █     █████    ███     ███    █   █           █████   █   █   █   █    ███    █████    ███     ███    █   █    ███
//   █ █    █   █     █       █     █   █    █ █      █       █     █   █   █   █           █       █   █   █   █   █   █     █       █     █   █   █   █   █   █
//  █   █   █         █       █     █   █   █   █     █       █     █   █   ██  █           █       █   █   ██  █   █         █       █     █   █   ██  █   █
//  █   █   █         █       █     █   █   █   █     █       █     █   █   █ █ █           ████    █   █   █ █ █   █         █       █     █   █   █ █ █    ███
//  █████   █         █       █     █   █   █████     █       █     █   █   █  ██           █       █   █   █  ██   █         █       █     █   █   █  ██       █
//  █   █   █   █     █       █      █ █    █   █     █       █     █   █   █   █           █       █   █   █   █   █   █     █       █     █   █   █   █   █   █
//  █   █    ███      █      ███      █     █   █     █      ███     ███    █   █           █        ███    █   █    ███      █      ███     ███    █   █    ███


/// <summary>
/// List of supported activation functions.
/// </summary>
internal enum ActivationFunction
{
    None, Sigmoid, TanH, ReLU, LeakyReLU, SeLU, PReLU, Logistic, Identity, Step,
    SoftSign, SoftPlus, Gaussian, BENTIdentity, Bipolar, BipolarSigmoid, HardTanH,
    Absolute, Not, SQLNL, SiLU, ArcTan, GELU, ELU, InverseSqrt, LeCunTanH, LogSigmoid,
    LogLog, Sin, Mish, Swish, TanhShrink, CELU
};

/// <summary>
/// For more information on activation functions, see:
/// https://machinelearninggeek.com/activation-functions/
/// https://en.wikipedia.org/wiki/Activation_function
/// https://stats.stackexchange.com/questions/115258/comprehensive-list-of-activation-functions-in-neural-networks-with-pros-cons
/// </summary>
internal static class ActivationUtils
{
    #region DELEGATE
    /// <summary>
    /// 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private delegate double ActivationDelegateMethod(double input);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private delegate double ActivationDerivativeDelegateMethod(double input);
    #endregion

    /// <summary>
    /// Mapping of function to method that computes it.
    /// </summary>
    private readonly static Dictionary<ActivationFunction, ActivationDelegateMethod> activationFunctionMap = new();

    /// <summary>
    /// Setup our mappings.
    /// </summary>
    static ActivationUtils()
    {
        activationFunctionMap.Add(ActivationFunction.Not, NotActivationFunction);
        activationFunctionMap.Add(ActivationFunction.Sigmoid, SigmoidActivationFunction);
        activationFunctionMap.Add(ActivationFunction.TanH, TanHActivationFunction);
        activationFunctionMap.Add(ActivationFunction.ReLU, ReLUActivationFunction);
        activationFunctionMap.Add(ActivationFunction.SeLU, SeLUActivationFunction);
        activationFunctionMap.Add(ActivationFunction.LeakyReLU, LeakyReLUActivationFunction);
        activationFunctionMap.Add(ActivationFunction.PReLU, PReLUActivationFunction);
        activationFunctionMap.Add(ActivationFunction.Logistic, LogisticActivationFunction);
        activationFunctionMap.Add(ActivationFunction.Identity, IdentityActivationFunction); // f(x) = x        
        activationFunctionMap.Add(ActivationFunction.Step, StepActivationFunction);
        activationFunctionMap.Add(ActivationFunction.SoftSign, SoftSignActivationFunction);
        activationFunctionMap.Add(ActivationFunction.SoftPlus, SoftPlusActivationFunction);
        activationFunctionMap.Add(ActivationFunction.Gaussian, GaussianActivationFunction);
        activationFunctionMap.Add(ActivationFunction.BENTIdentity, BENTIdentityActivationFunction);
        activationFunctionMap.Add(ActivationFunction.Bipolar, BipolarActivationFunction);
        activationFunctionMap.Add(ActivationFunction.BipolarSigmoid, BipolarSigmoidActivationFunction);
        activationFunctionMap.Add(ActivationFunction.HardTanH, HardTanHActivationFunction);
        activationFunctionMap.Add(ActivationFunction.Absolute, AbsoluteActivationFunction);
        activationFunctionMap.Add(ActivationFunction.SQLNL, SQLNLActivationFunction);
        activationFunctionMap.Add(ActivationFunction.SiLU, SiLUActivationFunction);
        activationFunctionMap.Add(ActivationFunction.ArcTan, ArcTanActivationFunction);
        activationFunctionMap.Add(ActivationFunction.GELU, GELUActivationFunction);
        activationFunctionMap.Add(ActivationFunction.ELU, ELUActivationFunction);
        activationFunctionMap.Add(ActivationFunction.InverseSqrt, InverseSqrtActivationFunction);
        activationFunctionMap.Add(ActivationFunction.LeCunTanH, LeCunTanHActivationFunction);
        activationFunctionMap.Add(ActivationFunction.LogSigmoid, LogSigmoidActivationFunction);
        activationFunctionMap.Add(ActivationFunction.LogLog, LogLogActivationFunction);
        activationFunctionMap.Add(ActivationFunction.Sin, SinActivationFunction);
        activationFunctionMap.Add(ActivationFunction.Mish, MishActivationFunction);
        activationFunctionMap.Add(ActivationFunction.Swish, SwishActivationFunction);
        activationFunctionMap.Add(ActivationFunction.TanhShrink, TanhShrinkActivationFunction);
        activationFunctionMap.Add(ActivationFunction.CELU, CELUActivationFunction);
    }

    /// <summary>
    /// Invoke the activation function.
    /// </summary>
    /// <param name="activationFunction"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    internal static double Activate(ActivationFunction activationFunction, double value)
    {
        if (activationFunction == ActivationFunction.None) return value;

        if (activationFunctionMap.TryGetValue(activationFunction, out ActivationDelegateMethod? method)) return method(value);

        throw new ArgumentException("activation function is not recognised", nameof(activationFunction));
    }

    /// <summary>
    /// Tanh squashes a real-valued number to the range [-1, 1]. It’s non-linear. 
    /// But unlike Sigmoid, its output is zero-centered. Therefore, in practice the tanh non-linearity is always preferred 
    /// to the sigmoid nonlinearity.
    /// 
    /// Activate is TANH         1_       ___
    /// (hyperbolic tangent)     0_      /
    ///                         -1_  ___/
    ///                                | | |
    ///                     -infinity -2 0 2..infinity
    ///                               
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double TanHActivationFunction(double x)
    {
        return (double)Math.Tanh(x); // f(x) = tanh(x) = 2/(1+e^-2x)-1
    }

    /// <summary>
    /// Sigmoid takes a real value as input and outputs another value between 0 and 1. 
    /// It’s easy to work with and has all the nice properties of activation functions: 
    /// it’s non-linear, continuously differentiable, monotonic, and has a fixed output range.
    /// 
    /// Pros
    /// - It is nonlinear in nature. Combinations of this function are also nonlinear!
    /// - It will give an analog activation unlike step function.
    /// - It has a smooth gradient too.
    /// - It’s good for a classifier.
    /// - The output of the activation function is always going to be in range (0,1) compared to (-inf, inf) of linear function.
    ///   So we have our activations bound in a range - so it won’t blow up the activations then.
    /// 
    /// Cons
    /// - Towards either end of the sigmoid function, the Y values tend to respond very less to changes in X.
    /// - It gives rise to a problem of “vanishing gradients”.
    /// - Its output isn’t zero centered.It makes the gradient updates go too far in different directions. 0 < output< 1, and it 
    ///   makes optimization harder.
    /// - Sigmoids saturate and kill gradients.
    /// - The network refuses to learn further or is drastically slow (depending on use case and until gradient /computation gets
    ///   hit by floating point value limits).
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double SigmoidActivationFunction(double x)
    {
        double k = (double)Math.Exp(-x); // f(x) = 1/(1+e^-x)  or e^x/(e^x + 1)
        return 1 / (k + 1);
    }

    /// <summary>
    /// The rectified linear activation function or ReLU for short is a piecewise linear function that will output 
    /// the input directly if it is positive, otherwise, it will output zero. It has become the default activation 
    /// function for many types of neural networks because a model that uses it is easier to train and often achieves 
    /// better performance.
    /// 
    /// Literally, if input <0 returns 0 else returns input. i.e. loses anything negative.
    /// 
    /// See: https://machinelearningmastery.com/rectified-linear-activation-function-for-deep-learning-neural-networks/#:~:text=The%20rectified%20linear%20activation%20function,otherwise%2C%20it%20will%20output%20zero.
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double ReLUActivationFunction(double x)
    {
        return x > 0 ? x : 0;
    }

    /// <summary>
    /// The rectified linear activation function or ReLU for short is a piecewise linear function that will output 
    /// the input directly if it is positive, otherwise, it will output zero. It has become the default activation 
    /// function for many types of neural networks because a model that uses it is easier to train and often achieves 
    /// better performance.
    /// 
    /// Literally, if input <0 returns 0 else returns input. i.e. loses anything negative.
    /// 
    /// See: https://machinelearningmastery.com/rectified-linear-activation-function-for-deep-learning-neural-networks/#:~:text=The%20rectified%20linear%20activation%20function,otherwise%2C%20it%20will%20output%20zero.
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double PReLUActivationFunction(double x)
    {
        return x > 0 ? x : RarelyModifiedSettings.Alpha * x;
    }

    /// <summary>
    /// Leaky Rectified Linear Unit, or Leaky ReLU, is a type of activation function based on a ReLU, but it has 
    /// a small slope for negative values instead of a flat slope. The slope coefficient is determined before 
    /// training, i.e. it is not learnt during training. This type of activation function is popular in tasks 
    /// where we we may suffer from sparse gradients, for example training generative adversarial networks.
    /// 
    /// See: https://paperswithcode.com/method/leaky-relu#:~:text=Leaky%20Rectified%20Linear%20Unit%2C%20or,is%20not%20learnt%20during%20training.
    /// f(x) = x > 0 ? x : alpha * x
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    internal static double LeakyReLUActivationFunction(double x)
    {
        return Math.Max(RarelyModifiedSettings.Alpha * x, x); // check this, not sure if this is correct. if x=-1, with f(x), it should return -0.01, but with this it returns -1.
    }

    /// <summary>
    /// Logistic aka soft-step.
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double LogisticActivationFunction(double x)
    {
        return (double)(1 / (1 + Math.Exp(-x))); // f(x) = 1/(1+e^-x)
    }

    /// <summary>
    /// Returns input.
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double IdentityActivationFunction(double x)
    {
        return x; // f(x) = x
    }

    /// <summary>
    /// Returns 1 if positive else 0. The beauty is in making positive outputs return 1 rather a decimal number.
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double StepActivationFunction(double x)
    {
        return x >= 0 ? 1 : 0; // f(x) = 1 if x >= 0, or 0 if x < 0
    }

    /// <summary>
    /// f(x) = x/(1+|x|)
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double SoftSignActivationFunction(double x)
    {
        return x / (1 + Math.Abs(x));
    }

    /// <summary>
    /// f(x) = Log(1+e^x)
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double SoftPlusActivationFunction(double x)
    {
        return Math.Log(1 + Math.Exp(x));
    }

    /// <summary>
    /// f(x) = e^(-x^2))
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double GaussianActivationFunction(double x)
    {
        return (double)Math.Exp(-Math.Pow(x, 2));
    }

    /// <summary>
    /// f(x) = (sqrt(x^2+1)-1)/2+x
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double BENTIdentityActivationFunction(double x)
    {
        double d = (double)Math.Sqrt(Math.Pow(x, 2) + 1);
        return (d - 1) / 2 + x;
    }

    /// <summary>
    /// Bipolar function. 
    /// Returns 1 if positive else -1.
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double BipolarActivationFunction(double x)
    {
        return x > 0 ? 1 : -1;
    }

    /// <summary>
    /// f(x) = 2 / (1 + e^(-x)) - 1
    /// https://square.github.io/pysurvival/miscellaneous/activation_functions.html => f(x) = (1-e^x) / (1+e^x)
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double BipolarSigmoidActivationFunction(double x)
    {
        return (double)(2 / (1 + Math.Exp(-x)) - 1);
    }

    /// <summary>
    /// Whereas TANH flattens out at the ends, the HardTANH function is clipped at -1 and 1.
    /// f(x) = x>1?1:(x<-1?-1:x)
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double HardTanHActivationFunction(double x)
    {
        return Math.Max(-1, Math.Min(1, x));
    }

    /// <summary>
    /// ABS() activation function.
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double AbsoluteActivationFunction(double x)
    {
        return Math.Abs(x); // f(x) = Abs(x)
    }

    /// <summary>
    /// The closest thing to "NOT" is f(x) = 1-x.
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double NotActivationFunction(double x)
    {
        return 1 - x;
    }

    /// <summary>
    /// * SELU (Scaled Exponential Linear Unit) function: x -> 1.0507*ELU(1.67326, x)
    /// https://arxiv.org/pdf/1706.02515.pdf
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double SeLUActivationFunction(double x)
    {
        double alpha = 1.6732632423543772848170429916717;
        double scale = 1.0507009873554804934193349852946;
        double fx = x > 0 ? x : alpha * Math.Exp(x) - alpha;

        return (double)(fx * scale);
    }

    /// <summary>
    /// f(x) = x / (1 + e^-x)
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double SwishActivationFunction(double x)
    {
        return (double)(x / (1 + Math.Exp(-x)));
    }

    /// <summary>
    /// Mish
    /// f(x) = x*tanh(SoftPlus(x))
    /// See: https://github.com/digantamisra98/Mish
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double MishActivationFunction(double x)
    {
        return (double)(x * Math.Tanh(SoftPlusActivationFunction(x)));
    }

    /// <summary>
    /// Using SIN instead of TANH.
    /// f(x) = (x == 0) ? 1: (sin(x)/x)
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double SinActivationFunction(double x)
    {
        return (double)(x == 0 ? 1 : Math.Sin(x) / x);
    }

    /// <summary>
    /// LogLog activation function.
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double LogLogActivationFunction(double x)
    {
        return (double)(1 - Math.Exp(-Math.Exp(x)));
    }

    /// <summary>
    /// Log Sigmoid activation function.
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double LogSigmoidActivationFunction(double x)
    {
        return (double)Math.Log(1 / (1 + Math.Exp(-x)));
    }

    /// <summary>
    /// AI guru Yann LeCun's TANH function.
    /// I have no idea why this is better than the regular TANH function, but I am sure he has his reasons...
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double LeCunTanHActivationFunction(double x)
    {
        return 1.7159f * Math.Tanh(2 * x / 3);
    }

    /// <summary>
    /// ISRU (Inverse Square Root Unit)
    /// f(x) = x/sqrt(1+alpha*x^2)
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double InverseSqrtActivationFunction(double x)
    {
        return x / Math.Sqrt(1 + RarelyModifiedSettings.Alpha * Math.Pow(x, 2));
    }

    /// <summary>
    /// Error Linear Unit
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double ELUActivationFunction(double x)
    {
        return x > 0 ? x : RarelyModifiedSettings.Alpha * (Math.Exp(x) - 1);
    }

    /// <summary>
    /// Error function.
    /// </summary>
    /// <param name="x">in radians</param>
    /// <returns></returns>
    private static double Erf(double x)
    {
        double a1 = 0.254829592;
        double a2 = -0.284496736;
        double a3 = 1.421413741;
        double a4 = -1.453152027;
        double a5 = 1.061405429;
        double p = 0.3275911;

        double sign = x < 0 ? -1 : 1;
        x = Math.Abs(x);

        var t = 1.0 / (1.0 + p * x);
        var y = 1.0 - ((((a5 * t + a4) * t + a3) * t + a2) * t + a1) * t * Math.Exp(-Math.Pow(x, 2));

        return sign * y;
    }

    /// <summary>
    /// GELU (Gaussian error linear unit)
    /// f(x) = ( x / 2 )*(1 + Erf(x / sqrt(2)) )
    /// See: https://arxiv.org/abs/1606.08415
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double GELUActivationFunction(double x)
    {
        return x / 2 * (1 + Erf(x / Math.Sqrt(2)));
    }

    /// <summary>
    /// ATAN activation function.
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double ArcTanActivationFunction(double x)
    {
        return Math.Atan(x);
    }

    /// <summary>
    /// SQNL (Square nonlinearity).
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double SQLNLActivationFunction(double x)
    {
        if (x > 2) return 1;
        if (x < 2) return -1;

        return x + Math.Pow(x, 2) / 4;
    }

    /// <summary>
    /// Sigmoid linear unit (SiLU), or Swish1 function: x -> (x/(1+e^-x))
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double SiLUActivationFunction(double x)
    {
        return x * SigmoidActivationFunction(x);
    }

    /// <summary>
    /// TANH Shrink activation function.
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double TanhShrinkActivationFunction(double x)
    {
        return x - Math.Tanh(x);
    }

    /// <summary>
    /// CELU activation function.
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double CELUActivationFunction(double x)
    {
        return Math.Max(0, x) + Math.Min(0, RarelyModifiedSettings.Alpha * Math.Exp(x / RarelyModifiedSettings.Alpha) - 1);
    }
}
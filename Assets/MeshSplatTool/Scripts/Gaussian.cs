using UnityEngine;
using System.Collections;

// original source http://haishibai.blogspot.fi/2009/09/image-processing-c-tutorial-4-gaussian.html

public class Gaussian
{
        public static double[,] Calculate1DSampleKernel(double deviation, int size)
        {
            double[,] ret = new double[size, 1];
            double sum = 0;
            int half = size / 2;
            for (int i = 0; i < size; i++)
            {
                    ret[i, 0] = 1 / (System.Math.Sqrt(2 * System.Math.PI) * deviation) * System.Math.Exp(-(i - half) * (i - half) / (2 * deviation * deviation));
				sum += ret[i, 0];
				sum += ret[i, 0];
            }
            return ret;
        }
        public static double[,] Calculate1DSampleKernel(double deviation)
        {
            int size = (int)System.Math.Ceiling(deviation * 3) * 2 + 1;
            return Calculate1DSampleKernel(deviation, size);
        }
        public static double[,] CalculateNormalized1DSampleKernel(double deviation)
        {
            return NormalizeMatrix(Calculate1DSampleKernel(deviation));
        }
        public static double[,] NormalizeMatrix(double[,] matrix)
        {
            double[,] ret = new double[matrix.GetLength(0), matrix.GetLength(1)];
            double sum = 0;
            for (int i = 0; i < ret.GetLength(0); i++)
            {
                for (int j = 0; j < ret.GetLength(1); j++)
                    sum += matrix[i,j];
            }
            if (sum != 0)
            {
                for (int i = 0; i < ret.GetLength(0); i++)
                {
                    for (int j = 0; j < ret.GetLength(1); j++)
                        ret[i, j] = matrix[i,j] / sum;
                }
            }
            return ret;
        }
		
	public static double[,] GaussianConvolution(double[,] matrix, double deviation)
        {
            double[,] kernel = CalculateNormalized1DSampleKernel(deviation);
            double[,] res1 = new double[matrix.GetLength(0), matrix.GetLength(1)];
            double[,] res2 = new double[matrix.GetLength(0), matrix.GetLength(1)];
            //x-direction
            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                    res1[i, j] = processPoint(matrix, i, j, kernel, 0);
            }
            //y-direction
            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                    res2[i, j] = processPoint(res1, i, j, kernel, 1);
            }
            return res2;
        }
        private static double processPoint(double[,] matrix, int x, int y, double[,] kernel, int direction)
        {
            double res = 0;
            int half = kernel.GetLength(0) / 2;
            for (int i = 0; i < kernel.GetLength(0); i++)
            {
                int cox = direction == 0 ? x + i - half : x;
                int coy = direction == 1 ? y + i - half : y;
                if (cox >= 0 && cox < matrix.GetLength(0) && coy >= 0 && coy < matrix.GetLength(1))
                {
                    res += matrix[cox, coy] * kernel[i, 0];
                }
            }
            return res;
        }		
		
	private static Color grayscaler(Color cr)
	{
		return new Color(cr.r * .3f + cr.g * .59f + cr.b * 0.11f,  cr.r * .3f + cr.g * .59f + cr.b * 0.11f,  cr.r * .3f + cr.g * .59f + cr.b * 0.11f, cr.a);
//		return cr.grayscale;
	}
	
	public static Texture2D FilterProcessImage(double d, Texture2D image)
	{
				Texture2D ret = new Texture2D(image.width, image.height,TextureFormat.ARGB32, true);
		
				// red
				double[,] matrixR = new double[image.width, image.height];
				double[,] matrixG = new double[image.width, image.height];
				double[,] matrixB = new double[image.width, image.height];
				double[,] matrixA = new double[image.width, image.height];
		
				for (int i = 0; i < image.width; i++)
				{
					for (int j = 0; j < image.height; j++)
					{
						//matrixR[i, j] = grayscaler( image.GetPixel(i, j) ).r;
						Color c = image.GetPixel(i, j);
						matrixR[i, j] = c.r;
						matrixG[i, j] = c.g;
						matrixB[i, j] = c.b;
						matrixA[i, j] = c.a;
					}
				}
				matrixR = Gaussian.GaussianConvolution(matrixR, d);
				matrixG = Gaussian.GaussianConvolution(matrixG, d);
				matrixB = Gaussian.GaussianConvolution(matrixB, d);
				matrixA = Gaussian.GaussianConvolution(matrixA, d);
				
				
				for (int i = 0; i < image.width; i++)
				{
					for (int j = 0; j < image.height; j++)
					{
						float valR = (float)System.Math.Min(1.0f, matrixR[i,j]);
						float valG = (float)System.Math.Min(1.0f, matrixG[i,j]);
						float valB = (float)System.Math.Min(1.0f, matrixB[i,j]);
						float valA = (float)System.Math.Min(1.0f, matrixA[i,j]);

						ret.SetPixel(i, j, new Color(valR,valG,valB,valA));
						//ret.SetPixel(i, j, Color.red);
					}
				}
				
				ret.Apply();
				
				return ret;
	}		
		
}
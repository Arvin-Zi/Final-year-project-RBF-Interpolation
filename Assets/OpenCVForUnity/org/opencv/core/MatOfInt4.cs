using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;
using System;
using System.Collections.Generic;

namespace OpenCVForUnity.CoreModule
{
    public class MatOfInt4 : Mat
    {
        // 32SC4
        private const int _depth = CvType.CV_32S;
        private const int _channels = 4;

        public MatOfInt4()
            : base()
        {

        }

        protected MatOfInt4(IntPtr addr)
            : base(addr)
        {

            if (!empty() && checkVector(_channels, _depth) < 0)
                throw new CvException("Incompatible Mat");
            //FIXME: do we need release() here?
        }

        public static MatOfInt4 fromNativeAddr(IntPtr addr)
        {
            return new MatOfInt4(addr);
        }

        public MatOfInt4(Mat m)
            : base(m, Range.all())
        {
            if (m != null)
                m.ThrowIfDisposed();


            if (!empty() && checkVector(_channels, _depth) < 0)
                throw new CvException("Incompatible Mat");
            //FIXME: do we need release() here?
        }

        public MatOfInt4(params int[] a)
            : base()
        {

            fromArray(a);
        }

        public void alloc(int elemNumber)
        {
            if (elemNumber > 0)
                base.create(elemNumber, 1, CvType.makeType(_depth, _channels));
        }

        public void fromArray(params int[] a)
        {
            if (a == null || a.Length == 0)
                return;
            int num = a.Length / _channels;
            alloc(num);

            if (isContinuous())
            {
                MatUtils.copyToMat<int>(a, this);
            }
            else
            {
                if (dims() <= 2)
                {
                    MatUtils.copyToMat<int>(a, this);
                }
                else
                {
                    put(0, 0, a); //TODO: check ret val!
                }
            }
        }

        public int[] toArray()
        {
            int num = checkVector(_channels, _depth);
            if (num < 0)
                throw new CvException("Native Mat has unexpected type or size: " + ToString());
            int[] a = new int[num * _channels];
            if (num == 0)
                return a;

            if (isContinuous())
            {
                MatUtils.copyFromMat<int>(this, a);
            }
            else
            {
                if (dims() <= 2)
                {
                    MatUtils.copyFromMat<int>(this, a);
                }
                else
                {
                    get(0, 0, a); //TODO: check ret val!
                }
            }
            return a;
        }

        public void fromList(List<int> lb)
        {
            if (lb == null || lb.Count == 0)
                return;

            int num = lb.Count / _channels;
            alloc(num);

            Converters.List_int_to_Mat(lb, this, num, CvType.CV_32SC4);
        }

        public List<int> toList()
        {
            int num = checkVector(_channels, _depth);
            if (num < 0)
                throw new CvException("Native Mat has unexpected type or size: " + ToString());

            List<int> a = new List<int>(num);
            for (int i = 0; i < num * _channels; i++)
            {
                a.Add(0);
            }
            Converters.Mat_to_List_int(this, a, num, CvType.CV_32SC4);
            return a;
        }
    }
}
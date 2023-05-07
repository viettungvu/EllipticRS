using System;
using System.Collections.Generic;
using System.Text;

namespace EllipticModels
{
    public enum Pharse
    {
        ALL=0,
        BUILD_SINH_KHOA_CONG_KHAI = 1,
        BUILD_SINH_KHOA_DUNG_CHUNG = 2,
        BUILD_MA_HOA_XEP_HANG = 3,
        BUILD_TINH_TONG_BAO_MAT = 4,

        SUGGEST_MA_HOA_VECTOR = 5,
        SUGGEST_MA_HOA_DO_TUONG_TU = 6,
        SUGGEST_DU_DOAN_XEP_HANG = 7,

        CALCULATE_SIMILAR=8,
    }

    public enum CustomSort
    {
        ALL=0,
        ASC=1,
        DESC=2
    }

    public enum LoaiPhim
    {
        ALL=0,
        PHIM=1,
        THE_LOAI_PHIM=2,
    }
}

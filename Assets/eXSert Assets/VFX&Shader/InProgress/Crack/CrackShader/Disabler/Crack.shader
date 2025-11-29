Shader "Custom/DisableZWrite"
{
    SubShader{
        Tags{
            "RenderType" = "Opague"
        }
        Pass{
            ZWrite Off
        }
    }
}

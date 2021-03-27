
public class FieldData
{
    public int sx = 0, sz = 0;
    public float[,,] layers = null;

    public FieldData(int sx, int sz, int xLen, int zLen, int layerCount)
    {
        this.sx = sx;
        this.sz = sz;
        layers = new float[xLen, zLen, layerCount];
    }
}

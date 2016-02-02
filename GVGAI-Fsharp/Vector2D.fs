namespace GVGAI

type Vector2d =
    struct
    val mutable x : float
    val mutable y : float
    end

    new(x, y) = {x=x;y=y}
    new(v: Vector2d) = {x=v.x;y=v.y}

    member t.copy() = new Vector2d(t.x,t.y)
    member t.set (v: Vector2d) = t.x <- v.x; t.y <- v.y
    member t.set(x, y) = t.x <- x; t.y <- y
    /// Sets the vector to zero
    member t.zero() = t.x <- 0.0; t.y <- 0.0

    override t.ToString() = sprintf "%f:%f" t.x t.y

    member t.add (v: Vector2d) =
        t.x <- t.x + v.x
        t.y <- t.y + v.y

    member t.add(x, y) =
        t.x <- t.x + x
        t.y <- t.y + y
    
    member t.add(v: Vector2d,w) =
        t.x <- t.x + v.x*w
        t.y <- t.y + v.y*w

    member t.wrap(w, h) =
        t.x <- (t.x + w) % w
        t.y <- (t.y + h) % h

    member t.subtract (v: Vector2d) =
        t.x <- t.x - v.x
        t.y <- t.y - v.y

    member t.subtract(x, y) =
        t.x <- t.x - x
        t.y <- t.y - y

    member t.mul fac =
        t.x <- t.x*fac
        t.y <- t.y*fac

    /// Rotates the vector by the given theta.
    /// theta is in radians.
    member t.rotate theta =
        let cosTheta = cos theta
        let sinTheta = sin theta

        t.x <- t.x * cosTheta - t.y * sinTheta
        t.y <- t.x * sinTheta + t.y * cosTheta

    /// t.x * v.x + t.y * v.y (scalar product)
    member t.scalarProduct (v: Vector2d) = t.x * v.x + t.y * v.y

    static member inline sqr x = x*x

    /// Returns the square distance from vector v.
    member t.sqDist (v: Vector2d) =
        let sqr = Vector2d.sqr
        sqr(t.x-v.x)+sqr(t.y-v.y)

    /// Returns the magnitude of the vector.
    member t.mag() =
        let sqr = Vector2d.sqr
        sqrt(sqr(t.x)+sqr(t.y))

    /// Returns the distance from v.
    member t.dist v =
        sqrt(t.sqDist(v))

    /// Returns the distance from (xx,yy)
    member t.dist(xx,yy) =
        let sqr = Vector2d.sqr
        sqrt(sqr(t.x-xx)+sqr(t.y-yy))

    member t.theta() = atan2 t.y t.x

    member t.normalise() =
        let mag = t.mag()
        if mag > 0.0 then
            t.x <- t.x/mag
            t.y <- t.y/mag
    
    /// t.x * v.x + t.y * v.y (dot product)
    /// Same as scalar product
    member inline t.dot v = t.scalarProduct v

    /// Returns a new unit vector based of the current. Does not mutate the current vector.
    /// If the magnitude of the current vector is zero, it returns (1.0,0.0)
    member t.unitVector() =
        let mag = t.mag()
        if mag > 0.0 then new Vector2d(t.x/mag,t.y/mag)
        else new Vector2d(1.0,0.0)
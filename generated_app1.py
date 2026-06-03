import threading

console_lock = threading.Lock()
a = 0
b = 0
b1 = 0
b2 = 0
c = 0

def thread_0_worker():
    global a, b, b1, b2, c
    state = 0
    while True:
        if state == 0:
            a = 13
            state = 1
        elif state == 1:
            with console_lock:
                print("a = " + str(a))
            state = 2
        elif state == 2:
            with console_lock:
                try:
                    b = int(input("Input b: "))
                except ValueError:
                    pass
            state = 3
        elif state == 3:
            if b == 13:
                state = 5
            else:
                state = 4
        elif state == 4:
            b2 = 420
            state = 6
        elif state == 5:
            b1 = 42
            state = 7
        elif state == 6:
            with console_lock:
                print("b2 = " + str(b2))
            return
        elif state == 7:
            with console_lock:
                print("b1 = " + str(b1))
            return
        else:
            break

def thread_1_worker():
    global a, b, b1, b2, c
    state = 0
    while True:
        if state == 0:
            c = 1000
            state = 2
        elif state == 1:
            with console_lock:
                print("c = " + str(c))
            return
        elif state == 2:
            with console_lock:
                print("c = " + str(c))
            state = 1
        else:
            break

if __name__ == "__main__":
    threads = []
    t0 = threading.Thread(target=thread_0_worker)
    threads.append(t0)
    t0.start()
    t1 = threading.Thread(target=thread_1_worker)
    threads.append(t1)
    t1.start()
    t0.join()
    t1.join()

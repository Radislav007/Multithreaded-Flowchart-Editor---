import threading

console_lock = threading.Lock()
a = 0
b = 0
c = 0
d = 0

def thread_0_worker():
    global a, b, c, d
    state = 0
    while True:
        if state == 0:
            with console_lock:
                try:
                    a = int(input("Input a: "))
                except ValueError:
                    pass
            state = 1
        elif state == 1:
            with console_lock:
                print("a = " + str(a))
            state = 2
        elif state == 2:
            if a == 0:
                state = 4
            else:
                state = 6
        elif state == 3:
            with console_lock:
                print("c = " + str(c))
            return
        elif state == 4:
            c = 1234
            state = 3
        elif state == 5:
            with console_lock:
                print("d = " + str(d))
            return
        elif state == 6:
            d = 420
            state = 5
        else:
            break

def thread_1_worker():
    global a, b, c, d
    state = 0
    while True:
        if state == 0:
            b = 42
            state = 1
        elif state == 1:
            with console_lock:
                print("b = " + str(b))
            return
        else:
            break

def thread_2_worker():
    global a, b, c, d
    pass

if __name__ == "__main__":
    threads = []
    t0 = threading.Thread(target=thread_0_worker)
    threads.append(t0)
    t0.start()
    t1 = threading.Thread(target=thread_1_worker)
    threads.append(t1)
    t1.start()
    t2 = threading.Thread(target=thread_2_worker)
    threads.append(t2)
    t2.start()
    t0.join()
    t1.join()
    t2.join()

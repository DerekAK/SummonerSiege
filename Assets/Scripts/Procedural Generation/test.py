import sys
import time

def MergeSort(nums):
    
    def Merge(left, right):
        sorted_nums = []
        lp, rp = 0, 0
        while lp < len(left) or rp < len(right):
            # left element, right element, le, re
            le = None
            re = None
            if lp < len(left):
                le = left[lp]
            if rp < len(right):
                re = right[rp]
            if (le and re):
                if (le < re):
                    min = le
                    lp += 1
                else:
                    min = re
                    rp += 1
                sorted_nums.append(min)
            elif le:
                sorted_nums.append(le)
                lp += 1
            else:
                sorted_nums.append(re)
                rp += 1
        return sorted_nums

    if len(nums) <= 1:
        return nums

    mid = len(nums) // 2
    left = MergeSort(nums[ : mid])
    right = MergeSort(nums[mid : ])

    return Merge (left, right)


def FibonacciMemo(n, memo):
    if n == 0:
        return 0
    if n == 1:
        return 1

    if n-1 not in memo:
        memo[n-1] = FibonacciMemo(n-1, memo)

    if n-2 not in memo:
        memo[n-2] = FibonacciMemo(n-2, memo)
    
    return memo[n-1] + memo[n-2]


def Fibonacci(n):
    if n == 0:
        return 0
    if n == 1:
        return 1
    
    return Fibonacci(n-1) + Fibonacci(n-2)


def Palindrome(s):
    if len(s) <= 1:
        return True
    
    return s[0] == s[-1] and Palindrome(s[1:-1])


choices = [
    (MergeSort, list), (Fibonacci, int), (FibonacciMemo, int), (Palindrome, str)
]

def DisplayMenu():
    for index, choice in enumerate(choices):
        print(f"{index + 1}:", choices[index][0].__name__)
    choice = int(input("Choose a number: "))
    funct = choices[choice - 1][0]

    arg = input(f"Choose a parameter of type {choices[choice - 1][1].__name__}: ")

    return (funct, arg)

funct, arg = DisplayMenu()

if (funct == FibonacciMemo):
    print(funct(arg, {}))

else:
    print(funct(arg))


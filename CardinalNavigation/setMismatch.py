  class Solution:
    def findErrorNums(self, nums: List[int]) -> List[int]:
        maxValue: int = len(nums)
        minValue: int = 1
            
        # find number that's missing
        allNumbers: set = set(range(minValue, maxValue+1))
        try:
            missingNo: int = list(allNumbers.difference(set(nums)))[0]
        except:
            missingNo: int = None
                
        # find number that's duplicated
        duplicate = None
        seenNumbers = set()
        for num in nums:
            if num not in seenNumbers:
                seenNumbers.add(num)
            else:
                duplicate = num
        if duplicate is None or missingNo is None:
            return []
        return [duplicate, missingNo]
                    
